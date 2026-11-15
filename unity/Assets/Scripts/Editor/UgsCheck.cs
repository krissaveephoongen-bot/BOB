// Check for the UGS Relay + Lobby setup (art/docs/27). Unity Services only initialize in Play Mode, so this
// opens an empty scene, enters Play, then: anonymous sign-in -> relay allocation -> create and delete a lobby.
//   -executeMethod Bob.EditorTools.UgsCheck.RunBatch     (no -batchmode; exit 0 = PASS)
using System;
using System.Threading.Tasks;
using Bob.Networking;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Relay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bob.EditorTools
{
    public static class UgsCheck
    {
        const string ArmKey = "Bob.UgsCheck.Arm";
        const string BatchKey = "Bob.UgsCheck.Batch";

        [MenuItem("Bob/Run UGS Check")]
        public static void Run() => Arm(false);

        public static void RunBatch() => Arm(true);

        static void Arm(bool batch)
        {
            SessionState.SetBool(ArmKey, true);
            SessionState.SetBool(BatchKey, batch);
            // Automation runs on an empty scene; the menu path must not replace the scene the owner has open.
            if (batch) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        /// <summary>Play mode reloads the domain and drops the update hook, so re-arm it from SessionState.</summary>
        [InitializeOnLoadMethod]
        static void Rehook()
        {
            if (!SessionState.GetBool(ArmKey, false)) return;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static Task<bool> running;

        static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            EditorApplication.update -= Tick;
            running = CheckAsync();
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (!running.IsCompleted) return;
            EditorApplication.update -= Poll;
            SessionState.SetBool(ArmKey, false);
            bool ok = running.Status == TaskStatus.RanToCompletion && running.Result;
            if (SessionState.GetBool(BatchKey, false)) EditorApplication.Exit(ok ? 0 : 1);
            else EditorApplication.ExitPlaymode();
        }

        static async Task<bool> CheckAsync()
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                    await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("[UGSCHK] auth ok · player " + AuthenticationService.Instance.PlayerId);

                var allocation = await RelayService.Instance.CreateAllocationAsync(1);
                string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                Debug.Log($"[UGSCHK] relay ok · {allocation.RelayServer.IpV4}:{allocation.RelayServer.Port} · code {code}");

                var lobby = await LobbyService.Instance.CreateLobbyAsync("bob-ugs-check", 2);
                Debug.Log("[UGSCHK] lobby ok · " + lobby.LobbyCode);
                await LobbyService.Instance.DeleteLobbyAsync(lobby.Id);

                await CheckHostPhaseAsync();

                Debug.Log("[UGSCHK] UGS CHECK PASSED");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[UGSCHK] UGS CHECK FAILED · " + e);
                return false;
            }
        }

        /// <summary>Builds a runtime NetworkManager and drives the real RelayLobbyManager host path.</summary>
        static async Task CheckHostPhaseAsync()
        {
            var networkGo = new GameObject("UgsCheckNetwork");
            var nm = networkGo.AddComponent<NetworkManager>();
            // AddComponent at runtime skips Reset(), so NetworkConfig is null until we create it.
            nm.NetworkConfig = new NetworkConfig();
            var utp = networkGo.AddComponent<UnityTransport>();
            nm.NetworkConfig.NetworkTransport = utp;

            var bootstrapGo = new GameObject("UgsCheckBootstrap");
            var bootstrap = bootstrapGo.AddComponent<UgsBootstrapManager>();

            var lobbyGo = new GameObject("UgsCheckLobby");
            var manager = lobbyGo.AddComponent<RelayLobbyManager>();
            manager.networkManager = nm;
            manager.transport = utp;
            manager.bootstrap = bootstrap;

            try
            {
                bool created = await manager.CreateGameAsync("bob-ugs-check", 4);
                if (!created || !nm.IsHost || string.IsNullOrEmpty(manager.RelayJoinCode))
                    throw new Exception("host phase failed · " + manager.Status);
                string lobbyId = manager.CurrentLobby.Id;
                Debug.Log($"[UGSCHK] host ok · lobby {manager.CurrentLobby.LobbyCode} · relay {manager.RelayJoinCode}");

                await manager.LeaveOrShutdownAsync();
                // Shutdown finishes on the next network update, not inside the call.
                for (float waited = 0f; nm.IsListening && waited < 2f; waited += 0.05f)
                    await Task.Delay(50);
                if (nm.IsListening) throw new Exception("transport still listening after leave");

                try { await LobbyService.Instance.GetLobbyAsync(lobbyId); throw new Exception("lobby still exists after leave"); }
                catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyNotFound) { }

                Debug.Log("[UGSCHK] host leave ok · lobby deleted, transport shut down");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(lobbyGo);
                UnityEngine.Object.DestroyImmediate(bootstrapGo);
                UnityEngine.Object.DestroyImmediate(networkGo);
            }
        }
    }
}
