// Battle of the Brooms: throwaway IMGUI panel for testing Relay + Lobby before any real menu exists.
// Editor and development builds only — release builds compile it out.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Unity.Netcode;
using UnityEngine;

namespace Bob.Networking
{
    public class SimpleDebugLobbyUI : MonoBehaviour
    {
        public RelayLobbyManager manager;
        public string roomName = "BOB Test";
        public int maxPlayers = 4;
        public bool privateLobby;

        string joinCode = "";
        string feedback = "";

        void Awake()
        {
            if (manager == null) manager = RelayLobbyManager.Instance;
            if (manager == null) manager = FindAnyObjectByType<RelayLobbyManager>();
        }

        async void CreateRoom()
        {
            feedback = "";
            bool ok = await manager.CreateGameAsync(roomName, maxPlayers, privateLobby);
            if (ok) joinCode = manager.CurrentLobby.LobbyCode;
            else feedback = manager.Status;
        }

        async void JoinRoom()
        {
            feedback = "";
            bool ok = await manager.QuickJoinOrJoinByCodeAsync(string.IsNullOrWhiteSpace(joinCode) ? null : joinCode);
            if (ok) joinCode = manager.CurrentLobby.LobbyCode;
            else feedback = manager.Status;
        }

        async void Leave()
        {
            await manager.LeaveOrShutdownAsync();
            feedback = "";
        }

        void OnGUI()
        {
            if (manager == null) { GUI.Label(new Rect(20, 20, 420, 24), "RelayLobbyManager missing"); return; }

            GUI.Box(new Rect(20, 20, 430, 300), "UGS Relay + Lobby (debug)");
            GUILayout.BeginArea(new Rect(36, 48, 398, 260));

            GUILayout.Label("Room: " + roomName);
            GUILayout.Label("Players: " + maxPlayers + (privateLobby ? " · private" : " · public"));
            GUILayout.Label("Player: " + (UgsBootstrapManager.Instance != null && UgsBootstrapManager.Instance.PlayerId != null
                ? UgsBootstrapManager.Instance.PlayerId : "signing in…"));
            GUILayout.Space(4);
            GUILayout.Label("Status: " + manager.Status);
            GUILayout.Label("Lobby code: " + (manager.CurrentLobby != null ? manager.CurrentLobby.LobbyCode : "-"));
            GUILayout.Label("Relay code: " + (manager.RelayJoinCode ?? "-"));
            GUILayout.Label("Net: " + NetState());

            bool busy = manager.IsBusy || NetworkManager.Singleton == null;
            bool connected = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUI.enabled = !busy && !connected;
            if (GUILayout.Button("Create Room", GUILayout.Height(36))) CreateRoom();
            if (GUILayout.Button("Quick Join", GUILayout.Height(36))) JoinRoom();
            GUI.enabled = !busy && connected;
            if (GUILayout.Button("Leave", GUILayout.Height(36))) Leave();
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Join code (blank = quick join)");
            joinCode = GUILayout.TextField(joinCode ?? "", 12, GUILayout.Height(30));
            if (!string.IsNullOrEmpty(feedback)) GUILayout.Label(feedback);

            GUILayout.EndArea();
        }

        static string NetState()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return "no NetworkManager";
            if (nm.IsHost) return "HOST";
            if (nm.IsClient) return nm.IsConnectedClient ? "CLIENT connected" : "CLIENT connecting…";
            return "idle";
        }
    }
}
#endif
