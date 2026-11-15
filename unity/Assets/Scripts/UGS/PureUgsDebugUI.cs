// Battle of the Brooms: throwaway IMGUI panel that exercises the whole pure-UGS stack.
// Editor and development builds only — release builds compile it out.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using UnityEngine;

namespace Bob.Ugs
{
    public class PureUgsDebugUI : MonoBehaviour
    {
        string lobbyCode = "";
        string feedback = "";

        void OnGUI()
        {
            var bootstrap = UgsBootstrapManager.Instance;
            var save = UgsCloudSaveManager.Instance;
            var board = UgsLeaderboardManager.Instance;
            var relay = UgsRelayLobbyManager.Instance;

            GUILayout.BeginArea(new Rect(10, 10, 460, 470), GUI.skin.box);
            GUILayout.Label("<b>Pure UGS debug</b>", RichLabel());
            GUILayout.Label("PlayerId: " + (AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : "–"));
            GUILayout.Label("Signed in: " + (bootstrap != null && bootstrap.IsReady));

            int coins = save != null ? save.Profile.coins : 0;
            int level = save != null ? save.Profile.level : 0;
            int items = save != null ? save.Profile.unlockedItems.Count : 0;
            GUILayout.Label($"Coins: {coins}   Level: {level}   Items: {items}");

            GUILayout.Space(6);
            GUI.enabled = save != null;
            if (GUILayout.Button("+100 Coins (Cloud Save)")) _ = AddCoinsAsync();
            if (GUILayout.Button("Unlock \"test_item\" (Cloud Save)")) _ = UnlockAsync();
            GUI.enabled = board != null;
            if (GUILayout.Button("Submit Random Score & View Top 10 (Leaderboard)")) _ = ScoreAsync();
            GUI.enabled = true;

            GUILayout.Space(6);
            GUILayout.Label("Lobby code: " + (relay != null && relay.CurrentLobby != null ? relay.CurrentLobby.LobbyCode : "–"));
            GUILayout.Label("Relay code: " + (relay != null && !string.IsNullOrEmpty(relay.RelayJoinCode) ? relay.RelayJoinCode : "–"));
            GUILayout.Label("Status: " + (relay != null ? relay.Status : "no relay manager") + NetState());

            GUI.enabled = relay != null && !relay.IsBusy;
            if (GUILayout.Button("Create Room")) _ = CreateRoomAsync();
            lobbyCode = GUILayout.TextField(lobbyCode ?? "", 12);
            if (GUILayout.Button("Quick Join")) _ = JoinAsync();
            GUI.enabled = relay != null && relay.CurrentLobby != null;
            if (GUILayout.Button("Leave")) _ = LeaveAsync();
            GUI.enabled = true;

            if (!string.IsNullOrEmpty(feedback)) GUILayout.Label(feedback);
            GUILayout.EndArea();
        }

        static GUIStyle RichLabel()
        {
            var style = new GUIStyle(GUI.skin.label) { richText = true };
            return style;
        }

        static string NetState()
        {
            var nm = Unity.Netcode.NetworkManager.Singleton;
            if (nm == null) return "";
            if (nm.IsHost) return " · HOST";
            if (nm.IsClient && nm.IsConnectedClient) return " · CLIENT connected";
            if (nm.IsClient) return " · CLIENT connecting…";
            return "";
        }

        async System.Threading.Tasks.Task AddCoinsAsync()
        {
            if (await UgsCloudSaveManager.Instance.AddCoinsAndSaveAsync(100))
                feedback = "saved · coins " + UgsCloudSaveManager.Instance.Profile.coins;
            else feedback = "save failed · " + UgsCloudSaveManager.Instance.LastError;
        }

        async System.Threading.Tasks.Task UnlockAsync()
        {
            if (await UgsCloudSaveManager.Instance.UnlockItemAndSaveAsync("test_item"))
                feedback = "saved · items " + UgsCloudSaveManager.Instance.Profile.unlockedItems.Count;
            else feedback = "save failed · " + UgsCloudSaveManager.Instance.LastError;
        }

        async System.Threading.Tasks.Task ScoreAsync()
        {
            var board = UgsLeaderboardManager.Instance;
            double score = UnityEngine.Random.Range(100, 1000);
            if (!await board.SubmitScoreAsync(score)) { feedback = "submit failed · " + board.LastError; return; }

            var page = await board.GetTopScoresAsync(10);
            if (page == null) { feedback = "read failed · " + board.LastError; return; }

            var lines = new List<string>();
            foreach (var entry in page.Results)
                lines.Add($"{entry.Rank + 1}. {entry.PlayerName} {entry.Score:0}");
            feedback = $"submitted {score:0}\n" + string.Join("\n", lines);
            Debug.Log("[UGS] top scores\n" + feedback);
        }

        async System.Threading.Tasks.Task CreateRoomAsync()
        {
            feedback = "creating room…";
            bool ok = await UgsRelayLobbyManager.Instance.CreateRoomAsync("bob-room", 4);
            feedback = ok ? "room up · " + UgsRelayLobbyManager.Instance.CurrentLobby.LobbyCode : "create failed";
        }

        async System.Threading.Tasks.Task JoinAsync()
        {
            feedback = "joining…";
            bool ok = await UgsRelayLobbyManager.Instance.QuickJoinOrJoinByCodeAsync(lobbyCode);
            feedback = ok ? "joined · " + UgsRelayLobbyManager.Instance.CurrentLobby.LobbyCode : "join failed";
        }

        async System.Threading.Tasks.Task LeaveAsync()
        {
            await UgsRelayLobbyManager.Instance.LeaveRoomAsync();
            feedback = "left";
        }
    }
}
#endif
