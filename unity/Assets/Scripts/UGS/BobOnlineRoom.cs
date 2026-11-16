// Battle of the Brooms: the playable online room (docs 27 section 10). Host creates a Relay allocation + Lobby, clients
// join by the 6-character lobby code (or quick join), and both sides talk Netcode named messages. There are no prefab
// spawns: Relay/Netcode carry (a) a match start with the host's seed and (b) the hero pose plus task events, so the
// TaskRace scene does not need rebuilding and both devices draw the same task list.
//
// Node: this assembly is excluded from unity-typecheck (the UGS SDK has no public NuGet reference), so the only checks
// are Unity's own compile. Keep every call to APIs the other files in this folder already use.
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bob.Ugs
{
    /// <summary>
    /// A player-hosted room of 2-4 offline builds: Lobby for the listing and the code, Relay for the transport, Netcode
    /// named messages for everything the match needs. Survives the scene load (DontDestroyOnLoad) so the connection made
    /// in the lobby is the one the TaskRace scene runs on.
    /// </summary>
    public sealed class BobOnlineRoom : MonoBehaviour
    {
        public const string MatchScene = "TaskRace";
        public const int MaxSlots = 4;
        const string RelayJoinCodeKey = "RelayJoinCode";
        const string HeroKey = "hero";
        const string PoseMessage = "bob.pose";
        const string TaskMessage = "bob.task";
        const string StartMessage = "bob.start";
        const string ConnectionType = "dtls";
        const float HeartbeatSeconds = 15f;
        const float RosterSeconds = 2f;

        public static BobOnlineRoom Instance { get; private set; }

        /// <summary>Get the room singleton, creating its GameObject (and surviving scene loads) on first use.</summary>
        public static BobOnlineRoom Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("BobOnlineRoom");
            DontDestroyOnLoad(go);
            return go.AddComponent<BobOnlineRoom>();
        }

        public bool InRoom => lobby != null;
        public bool IsHost => lobby != null && lobby.HostId == OwnPlayerId;
        /// <summary>Host is connected as soon as it listens; a client only when Netcode says the handshake finished.</summary>
        public bool Connected => nm != null && (nm.IsHost ? nm.IsListening : nm.IsConnectedClient);
        /// <summary>This device's Netcode id (the sender id on its own messages; -1 when not listening yet).</summary>
        public int LocalNetId => nm != null && nm.IsListening ? (int)nm.LocalClientId : -1;
        public int Players => lobby != null ? Mathf.Max(1, lobby.Players.Count) : Connected && nm != null ? nm.ConnectedClientsList.Count : 0;
        public int MaxPlayers => lobby != null ? lobby.MaxPlayers : 0;
        public string Code => lobby != null ? lobby.LobbyCode : null;
        public string Status { get; private set; } = "offline";
        public bool Starting { get; private set; }
        public int StartSeed { get; private set; }
        public string HeroId { get; private set; } = "sweep";
        /// <summary>Hero ids in lobby join order (host first) — the S15 room draws one portrait per entry.</summary>
        public IReadOnlyList<string> Roster => roster;

        /// <summary>(netId, world position, yaw) — the sender's hero, 10 times a second over the unreliable channel.</summary>
        public event Action<int, Vector3, float> PoseReceived;
        /// <summary>(netId, spotId, completed) — host-authoritative: a client's event is applied once, then rebroadcast.</summary>
        public event Action<int, string, bool> TaskReceived;

        NetworkManager nm;
        UnityTransport transport;
        Lobby lobby;
        bool busy, handlersHooked;
        CancellationTokenSource loops;
        readonly List<string> roster = new List<string>();
        string pendingHero = "sweep";

        string OwnPlayerId => AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : null;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            loops?.Cancel();
            UnhookHandlers();
            if (Instance == this) Instance = null;
        }

        // ---------------------------------------------------------------- host / join / leave

        /// <summary>Host: Relay allocation, lobby with the join code in its data, then Netcode host and the 15 s heartbeat.</summary>
        public async Task<bool> HostAsync(string heroId, int maxPlayers)
        {
            if (busy) return false;
            busy = true;
            try
            {
                pendingHero = CleanHero(heroId);
                SetStatus("preparing");
                if (!await EnsureServicesAsync()) return false;

                var net = EnsureNetwork();
                if (net.IsListening) { SetStatus("already connected"); return false; }
                int slots = Mathf.Clamp(maxPlayers, 2, MaxSlots);

                SetStatus("creating relay allocation");
                // Relay counts connections: the host already occupies one of the room slots.
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(slots - 1);
                string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                ApplyRelayData(transport, allocation.ServerEndpoints, allocation.RelayServer.IpV4, allocation.RelayServer.Port,
                    allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, allocation.ConnectionData);

                SetStatus("creating lobby");
                lobby = await LobbyService.Instance.CreateLobbyAsync("bob-" + pendingHero, slots, new CreateLobbyOptions
                {
                    IsPrivate = false,
                    PlayerData = PlayerData(pendingHero),
                    Data = new Dictionary<string, DataObject>
                    {
                        { RelayJoinCodeKey, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                    }
                });

                if (!net.StartHost())
                {
                    await LobbyService.Instance.DeleteLobbyAsync(lobby.Id);
                    lobby = null;
                    SetStatus("StartHost failed · check the NetworkManager transport");
                    return false;
                }

                HeroId = pendingHero;
                HookHandlers(net);
                StartLoops();
                SetStatus("hosting");
                Debug.Log($"[UGS] room up · lobby {lobby.LobbyCode} · relay {relayCode} · {slots} slots");
                RefreshRoster();
                return true;
            }
            catch (LobbyServiceException e) { SetStatus("lobby error · " + e.Reason); Debug.LogError("[UGS] " + e); return false; }
            catch (RelayServiceException e) { SetStatus("relay error · " + e.Message); Debug.LogError("[UGS] " + e); return false; }
            catch (Exception e) { SetStatus("create failed · " + e.Message); Debug.LogError("[UGS] " + e); return false; }
            finally { busy = false; }
        }

        /// <summary>Client: quick join (empty code) or join by the 6-character lobby code, then the Relay handshake.</summary>
        public async Task<bool> JoinAsync(string heroId, string code)
        {
            if (busy) return false;
            busy = true;
            try
            {
                pendingHero = CleanHero(heroId);
                SetStatus("preparing");
                if (!await EnsureServicesAsync()) return false;

                var net = EnsureNetwork();
                if (net.IsListening) { SetStatus("already connected"); return false; }

                SetStatus(string.IsNullOrWhiteSpace(code) ? "quick joining" : "joining " + code.Trim().ToUpperInvariant());
                lobby = string.IsNullOrWhiteSpace(code)
                    ? await LobbyService.Instance.QuickJoinLobbyAsync(new QuickJoinLobbyOptions { PlayerData = PlayerData(pendingHero) })
                    : await LobbyService.Instance.JoinLobbyByCodeAsync(code.Trim().ToUpperInvariant(),
                        new JoinLobbyByCodeOptions { PlayerData = PlayerData(pendingHero) });

                if (lobby == null || lobby.Data == null || !lobby.Data.TryGetValue(RelayJoinCodeKey, out var entry) ||
                    string.IsNullOrEmpty(entry.Value))
                {
                    await DropLobbyMembership(lobby);
                    lobby = null;
                    SetStatus("this room has no relay code yet — try again in a second");
                    return false;
                }

                SetStatus("joining relay");
                JoinAllocation join = await RelayService.Instance.JoinAllocationAsync(entry.Value);
                ApplyRelayData(transport, join.ServerEndpoints, join.RelayServer.IpV4, join.RelayServer.Port,
                    join.AllocationIdBytes, join.Key, join.ConnectionData, join.HostConnectionData);

                if (!net.StartClient())
                {
                    await DropLobbyMembership(lobby);
                    lobby = null;
                    SetStatus("StartClient failed · check the NetworkManager transport");
                    return false;
                }

                HeroId = pendingHero;
                HookHandlers(net);
                StartLoops();
                SetStatus("joining");
                Debug.Log($"[UGS] joined room · lobby {lobby.LobbyCode}");
                RefreshRoster();
                return true;
            }
            catch (LobbyServiceException e) { SetStatus("lobby error · " + e.Reason); Debug.LogError("[UGS] " + e); return false; }
            catch (RelayServiceException e) { SetStatus("relay error · " + e.Message); Debug.LogError("[UGS] " + e); return false; }
            catch (Exception e) { SetStatus("join failed · " + e.Message); Debug.LogError("[UGS] " + e); return false; }
            finally { busy = false; }
        }

        /// <summary>Host deletes the lobby, a client leaves it, then Netcode shuts down and the scene can go back home.</summary>
        public async Task LeaveAsync()
        {
            loops?.Cancel(); loops = null;
            var leaving = lobby;
            lobby = null;
            Starting = false;

            if (leaving != null) await DropLobbyMembership(leaving);

            if (nm != null && nm.IsListening) nm.Shutdown();
            UnhookHandlers();
            roster.Clear();
            SetStatus("offline");
        }

        async Task DropLobbyMembership(Lobby target)
        {
            if (target == null) return;
            try
            {
                if (target.HostId == OwnPlayerId) await LobbyService.Instance.DeleteLobbyAsync(target.Id);
                else await LobbyService.Instance.RemovePlayerAsync(target.Id, OwnPlayerId);
            }
            catch (LobbyServiceException e)
            {
                // Already gone (expired, or the host deleted it) is fine while closing the session.
                if (e.Reason != LobbyExceptionReason.LobbyNotFound) Debug.LogWarning("[UGS] leave lobby: " + e.Message);
            }
            catch (Exception e) { Debug.LogWarning("[UGS] leave lobby: " + e.Message); }
        }

        // ---------------------------------------------------------------- match handoff

        /// <summary>
        /// Host presses start: the seed and the player count go out once (reliable), then every device loads the same
        /// scene and draws the same task list. Clients get here from <see cref="StartMessage"/>, not from their own UI.
        /// </summary>
        public void StartMatch(int seed, int players)
        {
            if (!InRoom || !IsHost || nm == null) return;
            Starting = true;
            StartSeed = seed;
            int slots = Mathf.Clamp(players, 1, MaxSlots);
            using (var writer = new FastBufferWriter(8, Allocator.Temp))
            {
                writer.WriteValueSafe(seed);
                writer.WriteValueSafe(slots);
                nm.CustomMessagingManager.SendNamedMessageToAll(StartMessage, writer, NetworkDelivery.ReliableSequenced);
            }
            EnterMatch(true, slots, seed);
        }

        void EnterMatch(bool host, int players, int seed)
        {
            Bob.Game.MatchRequest.SetOnline(host, players, seed);
            Debug.Log($"[UGS] match start · host {host} · {players} player(s) · seed {seed}");
            if (Application.CanStreamedLevelBeLoaded(MatchScene)) SceneManager.LoadScene(MatchScene);
            else { Debug.LogError("[UGS] " + MatchScene + " is not in Build Settings"); SetStatus("scene missing"); }
        }

        // ---------------------------------------------------------------- gameplay channel

        /// <summary>Local hero pose, 10 Hz, unreliable: losing a packet is invisible, a stale one is not.</summary>
        public void SendPose(Vector3 position, float yaw)
        {
            if (!Connected || nm == null) return;
            int origin = LocalNetId;
            using (var writer = new FastBufferWriter(16, Allocator.Temp))
            {
                // The origin id travels in the payload: the host relays a client's pose, so the client id on the wire is
                // the host's, not the sender's (three or four players would otherwise share one body).
                writer.WriteValueSafe(origin);
                writer.WriteValueSafe(position.x);
                writer.WriteValueSafe(position.z);
                writer.WriteValueSafe(yaw);
                if (nm.IsHost) nm.CustomMessagingManager.SendNamedMessageToAll(PoseMessage, writer, NetworkDelivery.Unreliable);
                else nm.CustomMessagingManager.SendNamedMessage(PoseMessage, NetworkManager.ServerClientId, writer, NetworkDelivery.Unreliable);
            }
        }

        /// <summary>
        /// A task was completed on this device. Clients send it to the host; the host applies it and rebroadcasts, so the
        /// two islands can never disagree about which stations are clean.
        /// </summary>
        public void SendTask(string spotId, bool completed)
        {
            if (!Connected || nm == null || string.IsNullOrEmpty(spotId)) return;
            byte[] id = Encoding.ASCII.GetBytes(spotId);
            using (var writer = new FastBufferWriter(id.Length + 12, Allocator.Temp))
            {
                writer.WriteValueSafe(LocalNetId);
                writer.WriteValueSafe(id);
                writer.WriteValueSafe(completed);
                if (nm.IsHost) nm.CustomMessagingManager.SendNamedMessageToAll(TaskMessage, writer, NetworkDelivery.ReliableSequenced);
                else nm.CustomMessagingManager.SendNamedMessage(TaskMessage, NetworkManager.ServerClientId, writer, NetworkDelivery.ReliableSequenced);
            }
        }

        // ---------------------------------------------------------------- messages

        void HookHandlers(NetworkManager net)
        {
            if (handlersHooked) return;
            if (net.CustomMessagingManager == null) { Debug.LogError("[UGS] no CustomMessagingManager yet — the room cannot talk"); return; }
            handlersHooked = true;
            net.OnClientConnectedCallback += OnPeerConnected;
            net.OnClientDisconnectCallback += OnPeerDisconnected;
            net.CustomMessagingManager.RegisterNamedMessageHandler(PoseMessage, HandlePose);
            net.CustomMessagingManager.RegisterNamedMessageHandler(TaskMessage, HandleTask);
            net.CustomMessagingManager.RegisterNamedMessageHandler(StartMessage, HandleStart);
        }

        void UnhookHandlers()
        {
            if (!handlersHooked || nm == null) return;
            handlersHooked = false;
            nm.OnClientConnectedCallback -= OnPeerConnected;
            nm.OnClientDisconnectCallback -= OnPeerDisconnected;
            if (nm.CustomMessagingManager == null) return;
            nm.CustomMessagingManager.UnregisterNamedMessageHandler(PoseMessage);
            nm.CustomMessagingManager.UnregisterNamedMessageHandler(TaskMessage);
            nm.CustomMessagingManager.UnregisterNamedMessageHandler(StartMessage);
        }

        void OnPeerConnected(ulong clientId)
        {
            if (nm != null && nm.IsHost) SetStatus("hosting · " + Players + "/" + MaxPlayers);
            Debug.Log($"[UGS] peer connected · client {clientId} · {Players} in room");
            RefreshRoster();
        }

        void OnPeerDisconnected(ulong clientId)
        {
            Debug.Log($"[UGS] peer left · client {clientId}");
            if (nm != null && nm.IsHost) SetStatus("hosting · " + Players + "/" + MaxPlayers);
            RefreshRoster();
        }

        void HandlePose(ulong sender, FastBufferReader reader)
        {
            if (nm == null) return;
            bool host = nm.IsHost;
            if (!host && sender != NetworkManager.ServerClientId) return;
            reader.ReadValueSafe(out int origin);
            reader.ReadValueSafe(out float x);
            reader.ReadValueSafe(out float z);
            reader.ReadValueSafe(out float yaw);
            if (origin == LocalNetId) return;
            if (host && sender != nm.LocalClientId) RebroadcastPose(origin, x, z, yaw);
            PoseReceived?.Invoke(origin, new Vector3(x, 0f, z), yaw);
        }

        void RebroadcastPose(int origin, float x, float z, float yaw)
        {
            using (var writer = new FastBufferWriter(16, Allocator.Temp))
            {
                writer.WriteValueSafe(origin);
                writer.WriteValueSafe(x);
                writer.WriteValueSafe(z);
                writer.WriteValueSafe(yaw);
                nm.CustomMessagingManager.SendNamedMessageToAll(PoseMessage, writer, NetworkDelivery.Unreliable);
            }
        }

        void HandleTask(ulong sender, FastBufferReader reader)
        {
            if (nm == null) return;
            // Clients only accept the host's word; the host accepts a client's report and passes it on.
            bool host = nm.IsHost;
            if (!host && sender != NetworkManager.ServerClientId) return;
            reader.ReadValueSafe(out int origin);
            reader.ReadValueSafe(out byte[] id);
            reader.ReadValueSafe(out bool completed);
            string spotId = id == null ? "" : Encoding.ASCII.GetString(id);
            if (origin == LocalNetId) return; // already applied locally, the originator must not apply it twice
            if (host && sender != nm.LocalClientId) RebroadcastTask(origin, id, completed);
            TaskReceived?.Invoke(origin, spotId, completed);
        }

        void RebroadcastTask(int origin, byte[] id, bool completed)
        {
            using (var writer = new FastBufferWriter((id == null ? 0 : id.Length) + 12, Allocator.Temp))
            {
                writer.WriteValueSafe(origin);
                writer.WriteValueSafe(id);
                writer.WriteValueSafe(completed);
                nm.CustomMessagingManager.SendNamedMessageToAll(TaskMessage, writer, NetworkDelivery.ReliableSequenced);
            }
        }

        void HandleStart(ulong sender, FastBufferReader reader)
        {
            if (nm == null || nm.IsHost) return;
            reader.ReadValueSafe(out int seed);
            reader.ReadValueSafe(out int players);
            StartSeed = seed;
            Starting = true;
            EnterMatch(false, players, seed);
        }

        // ---------------------------------------------------------------- loops

        void StartLoops()
        {
            loops?.Cancel();
            loops = new CancellationTokenSource();
            _ = HeartbeatLoopAsync(loops.Token);
            _ = RosterLoopAsync(loops.Token);
        }

        async Task HeartbeatLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try { await Task.Delay(TimeSpan.FromSeconds(HeartbeatSeconds), token); }
                catch (OperationCanceledException) { return; }
                if (lobby == null || !IsHost) return;
                try
                {
                    await LobbyService.Instance.SendHeartbeatPingAsync(lobby.Id);
                    SetStatus("hosting");
                }
                catch (LobbyServiceException e)
                {
                    SetStatus("heartbeat failed · " + e.Reason);
                    Debug.LogWarning("[UGS] heartbeat: " + e.Message);
                    return;
                }
            }
        }

        async Task RosterLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try { await Task.Delay(TimeSpan.FromSeconds(RosterSeconds), token); }
                catch (OperationCanceledException) { return; }
                if (lobby == null) return;
                try
                {
                    lobby = await LobbyService.Instance.GetLobbyAsync(lobby.Id);
                    RefreshRoster();
                }
                catch (LobbyServiceException e)
                {
                    if (e.Reason == LobbyExceptionReason.LobbyNotFound) { SetStatus("room closed"); lobby = null; return; }
                    Debug.LogWarning("[UGS] roster: " + e.Message);
                }
                catch (Exception e) { Debug.LogWarning("[UGS] roster: " + e.Message); }
            }
        }

        void RefreshRoster()
        {
            roster.Clear();
            if (lobby == null || lobby.Players == null) return;
            foreach (var player in lobby.Players)
            {
                string hero = "sweep";
                if (player.Data != null && player.Data.TryGetValue(HeroKey, out var entry) && !string.IsNullOrEmpty(entry.Value))
                    hero = entry.Value;
                roster.Add(hero);
            }
        }

        // ---------------------------------------------------------------- setup

        async Task<bool> EnsureServicesAsync()
        {
            var bootstrap = UgsBootstrapManager.Instance;
            if (bootstrap == null)
            {
                var go = new GameObject("UGS");
                bootstrap = go.AddComponent<UgsBootstrapManager>();
                bootstrap.signInOnAwake = false; // this room asks for the sign-in itself
            }
            await bootstrap.EnsureReadyAsync();
            if (!bootstrap.IsReady) { SetStatus(string.IsNullOrEmpty(bootstrap.LastError) ? "no services" : "services · " + bootstrap.LastError); return false; }
            return true;
        }

        NetworkManager EnsureNetwork()
        {
            if (nm != null) return nm;
            nm = NetworkManager.Singleton;
            if (nm == null)
            {
                var go = new GameObject("Network");
                nm = go.AddComponent<NetworkManager>();
                // AddComponent at runtime skips Reset(), so NetworkConfig is null until we create it (same as UgsCheck).
                nm.NetworkConfig = new NetworkConfig();
                DontDestroyOnLoad(go);
            }
            if (nm.NetworkConfig == null) nm.NetworkConfig = new NetworkConfig();
            transport = nm.GetComponent<UnityTransport>();
            if (transport == null) transport = nm.gameObject.AddComponent<UnityTransport>();
            nm.NetworkConfig.NetworkTransport = transport;
            nm.NetworkConfig.EnableSceneManagement = false; // every device loads TaskRace itself, no scene sync needed
            nm.NetworkConfig.ConnectionApproval = false;
            DontDestroyOnLoad(nm.gameObject);
            return nm;
        }

        static string CleanHero(string heroId) => string.IsNullOrEmpty(heroId) ? "sweep" : heroId;

        static Dictionary<string, PlayerDataObject> PlayerData(string heroId) =>
            new Dictionary<string, PlayerDataObject>
            {
                { HeroKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, CleanHero(heroId)) }
            };

        /// <summary>Writes the allocation into the transport, preferring the endpoint that advertises our connection type.</summary>
        static void ApplyRelayData(UnityTransport utp, List<RelayServerEndpoint> endpoints, string fallbackHost, int fallbackPort,
            byte[] allocationId, byte[] key, byte[] connectionData, byte[] hostConnectionData)
        {
            RelayServerEndpoint endpoint = endpoints != null
                ? endpoints.Find(e => e.ConnectionType == ConnectionType)
                : null;
            if (endpoint == null) Debug.LogWarning($"[UGS] no {ConnectionType} endpoint in the allocation, using the plain relay server");
            string host = endpoint != null ? endpoint.Host : fallbackHost;
            ushort port = (ushort)(endpoint != null ? endpoint.Port : fallbackPort);
            utp.SetRelayServerData(host, port, allocationId, key, connectionData, hostConnectionData, ConnectionType == "dtls");
        }

        void SetStatus(string s)
        {
            Status = s;
            Debug.Log("[UGS] " + s);
        }
    }
}
