// Battle of the Brooms: player-hosted rooms — Relay allocation + Lobby listing + Netcode host/client.
// Host: CreateRoomAsync -> relay join code stored in lobby data -> StartHost, then a 15 s heartbeat.
// Client: QuickJoinOrJoinByCodeAsync -> read the code back -> join allocation -> StartClient.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Bob.Ugs
{
    public class UgsRelayLobbyManager : MonoBehaviour
    {
        public const string RelayJoinCodeKey = "RelayJoinCode";
        const float HeartbeatSeconds = 15f;
        const string ConnectionType = "dtls";

        public static UgsRelayLobbyManager Instance { get; private set; }

        [Tooltip("Auto-filled from NetworkManager.Singleton when empty.")]
        public NetworkManager networkManager;
        [Tooltip("Auto-filled from the NetworkManager's transport when empty.")]
        public UnityTransport transport;

        public Lobby CurrentLobby { get; private set; }
        public string RelayJoinCode { get; private set; }
        public string Status { get; private set; } = "idle";
        public bool IsBusy { get; private set; }
        public bool IsHost => CurrentLobby != null && CurrentLobby.HostId == OwnPlayerId;

        string OwnPlayerId => AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : null;

        CancellationTokenSource heartbeat;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            heartbeat?.Cancel();
            if (Instance == this) Instance = null;
        }

        public async Task<bool> CreateRoomAsync(string roomName, int maxPlayers)
        {
            if (IsBusy) return false;
            IsBusy = true;
            try
            {
                SetStatus("initializing services…");
                if (!await EnsureServicesAsync()) return false;
                if (!CheckNetworkReady(out var nm, out var utp)) return false;

                SetStatus("creating relay allocation…");
                // Relay counts connections, so the host already occupies one of the room slots.
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(Mathf.Max(1, maxPlayers - 1));
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                ApplyRelayData(utp, allocation.ServerEndpoints, allocation.RelayServer.IpV4, allocation.RelayServer.Port,
                    allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, allocation.ConnectionData);

                SetStatus("creating lobby…");
                CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(roomName, maxPlayers, new CreateLobbyOptions
                {
                    IsPrivate = false,
                    Data = new Dictionary<string, DataObject>
                    {
                        { RelayJoinCodeKey, new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
                    }
                });

                if (!nm.StartHost())
                {
                    await LobbyService.Instance.DeleteLobbyAsync(CurrentLobby.Id);
                    CurrentLobby = null;
                    SetStatus("StartHost failed — check the Network Transport on the NetworkManager");
                    return false;
                }

                RelayJoinCode = joinCode;
                StartHeartbeat();
                SetStatus($"hosting · lobby {CurrentLobby.LobbyCode} · 1/{maxPlayers}");
                Debug.Log($"[UGS] host up · lobby {CurrentLobby.LobbyCode} · relay {joinCode}");
                return true;
            }
            catch (LobbyServiceException e) { SetStatus("lobby error · " + e.Reason); Debug.LogError("[UGS] " + e); return false; }
            catch (RelayServiceException e) { SetStatus("relay error · " + e.Message); Debug.LogError("[UGS] " + e); return false; }
            catch (Exception e) { SetStatus("create failed · " + e.Message); Debug.LogError("[UGS] " + e); return false; }
            finally { IsBusy = false; }
        }

        public async Task<bool> QuickJoinOrJoinByCodeAsync(string lobbyCode = "")
        {
            if (IsBusy) return false;
            IsBusy = true;
            try
            {
                SetStatus("initializing services…");
                if (!await EnsureServicesAsync()) return false;
                if (!CheckNetworkReady(out var nm, out var utp)) return false;

                SetStatus(string.IsNullOrWhiteSpace(lobbyCode) ? "quick joining…" : "joining lobby " + lobbyCode.Trim() + "…");
                CurrentLobby = string.IsNullOrWhiteSpace(lobbyCode)
                    ? await LobbyService.Instance.QuickJoinLobbyAsync()
                    : await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode.Trim());

                if (CurrentLobby.Data == null || !CurrentLobby.Data.TryGetValue(RelayJoinCodeKey, out var entry) ||
                    string.IsNullOrEmpty(entry.Value))
                {
                    await LobbyService.Instance.RemovePlayerAsync(CurrentLobby.Id, OwnPlayerId);
                    CurrentLobby = null;
                    SetStatus("lobby has no relay code — the host may still be starting");
                    return false;
                }

                SetStatus("joining relay " + entry.Value + "…");
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(entry.Value);
                ApplyRelayData(utp, joinAllocation.ServerEndpoints, joinAllocation.RelayServer.IpV4, joinAllocation.RelayServer.Port,
                    joinAllocation.AllocationIdBytes, joinAllocation.Key, joinAllocation.ConnectionData, joinAllocation.HostConnectionData);

                if (!nm.StartClient())
                {
                    await LobbyService.Instance.RemovePlayerAsync(CurrentLobby.Id, OwnPlayerId);
                    CurrentLobby = null;
                    SetStatus("StartClient failed — check the Network Transport on the NetworkManager");
                    return false;
                }

                RelayJoinCode = entry.Value;
                SetStatus($"client · lobby {CurrentLobby.LobbyCode} · {CurrentLobby.Players.Count}/{CurrentLobby.MaxPlayers}");
                Debug.Log($"[UGS] joined · lobby {CurrentLobby.LobbyCode} · relay {entry.Value}");
                return true;
            }
            catch (LobbyServiceException e) { SetStatus("lobby error · " + e.Reason); Debug.LogError("[UGS] " + e); return false; }
            catch (RelayServiceException e) { SetStatus("relay error · " + e.Message); Debug.LogError("[UGS] " + e); return false; }
            catch (Exception e) { SetStatus("join failed · " + e.Message); Debug.LogError("[UGS] " + e); return false; }
            finally { IsBusy = false; }
        }

        /// <summary>Host deletes the lobby, a client leaves it, then Netcode shuts down.</summary>
        public async Task LeaveRoomAsync()
        {
            heartbeat?.Cancel(); heartbeat = null;

            var lobby = CurrentLobby;
            bool wasHost = lobby != null && lobby.HostId == OwnPlayerId;
            CurrentLobby = null;
            RelayJoinCode = null;

            if (lobby != null)
            {
                try
                {
                    if (wasHost) await LobbyService.Instance.DeleteLobbyAsync(lobby.Id);
                    else await LobbyService.Instance.RemovePlayerAsync(lobby.Id, OwnPlayerId);
                }
                catch (LobbyServiceException e)
                {
                    // Already gone (expired, or the host deleted it) is fine while closing the session.
                    if (e.Reason != LobbyExceptionReason.LobbyNotFound)
                        Debug.LogWarning("[UGS] leave lobby: " + e.Message);
                }
                catch (Exception e) { Debug.LogWarning("[UGS] leave lobby: " + e.Message); }
            }

            var nm = networkManager != null ? networkManager : NetworkManager.Singleton;
            if (nm != null && nm.IsListening) nm.Shutdown();
            SetStatus("offline");
        }

        async Task<bool> EnsureServicesAsync()
        {
            var bootstrap = UgsBootstrapManager.Instance;
            if (bootstrap == null) { SetStatus("no UgsBootstrapManager in the scene"); return false; }
            await bootstrap.EnsureReadyAsync();
            return bootstrap.IsReady;
        }

        bool CheckNetworkReady(out NetworkManager nm, out UnityTransport utp)
        {
            nm = networkManager != null ? networkManager : NetworkManager.Singleton;
            utp = transport != null ? transport : nm != null ? nm.GetComponent<UnityTransport>() : null;
            if (nm == null) { SetStatus("no NetworkManager in the scene"); return false; }
            if (utp == null) { SetStatus("NetworkManager has no UnityTransport"); return false; }
            if (nm.IsListening) { SetStatus("already connected — leave first"); return false; }
            return true;
        }

        /// <summary>Writes the allocation into the transport, preferring the endpoint that advertises our connection type.</summary>
        static void ApplyRelayData(UnityTransport utp, List<RelayServerEndpoint> endpoints, string fallbackHost, int fallbackPort,
            byte[] allocationId, byte[] key, byte[] connectionData, byte[] hostConnectionData)
        {
            RelayServerEndpoint endpoint = endpoints != null
                ? endpoints.FirstOrDefault(e => e.ConnectionType == ConnectionType)
                : null;
            if (endpoint == null) Debug.LogWarning($"[UGS] no {ConnectionType} endpoint in the allocation, using the plain relay server");
            string host = endpoint != null ? endpoint.Host : fallbackHost;
            ushort port = (ushort)(endpoint != null ? endpoint.Port : fallbackPort);
            utp.SetRelayServerData(host, port, allocationId, key, connectionData, hostConnectionData, ConnectionType == "dtls");
        }

        void StartHeartbeat()
        {
            heartbeat?.Cancel();
            heartbeat = new CancellationTokenSource();
            _ = HeartbeatLoopAsync(heartbeat.Token);
        }

        async Task HeartbeatLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try { await Task.Delay(TimeSpan.FromSeconds(HeartbeatSeconds), token); }
                catch (OperationCanceledException) { return; }
                if (CurrentLobby == null || !IsHost) return;
                try
                {
                    await LobbyService.Instance.SendHeartbeatPingAsync(CurrentLobby.Id);
                    SetStatus($"hosting · lobby {CurrentLobby.LobbyCode} · {CurrentLobby.Players.Count}/{CurrentLobby.MaxPlayers}");
                }
                catch (LobbyServiceException e)
                {
                    SetStatus("heartbeat failed · " + e.Reason);
                    Debug.LogWarning("[UGS] heartbeat: " + e.Message);
                    return;
                }
            }
        }

        void SetStatus(string s)
        {
            Status = s;
            Debug.Log("[UGS] " + s);
        }
    }
}
