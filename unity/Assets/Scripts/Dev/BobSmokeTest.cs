// Battle of the Brooms: end-to-end check inside Unity. Add this to any GameObject in an empty scene and press Play.
// It signs in, creates a Nakama match, asks the server for that match's room, builds a legal stack and gets the
// server's score. Success = the Console shows "SMOKE TEST PASSED" with a score.
using System;
using System.Collections.Generic;
using System.Linq;
using Bob.Net;
using Bob.Rules;
using UnityEngine;

namespace Bob.Dev
{
    public sealed class BobSmokeTest : MonoBehaviour
    {
        [Tooltip("127.0.0.1 in the Editor. On a phone, use your PC's LAN IP (e.g. 192.168.1.20).")]
        public string host = "127.0.0.1";
        public int port = 7350;

        readonly List<string> lines = new List<string>();
        BobServer server;

        void Log(string s) { lines.Add(s); Debug.Log("[BobSmokeTest] " + s); }

        async void Start()
        {
            try
            {
                server = new BobServer("http", host, port);
                await server.SignInAsync(SystemInfo.deviceUniqueIdentifier + "-bob-smoke");
                Log("signed in as " + server.UserId);
                Log("ping: " + await server.PingAsync());

                await server.ConnectAsync();
                var match = await server.CreateMatchAsync();
                Log("match created: " + match.Id);

                int seed = BobServer.SeedFromMatchId(match.Id), round = 1;
                var room = JsonUtility.FromJson<Room>(await server.MakeRoundAsync(seed, round));
                Log($"round {round} enemies: {string.Join(",", room.enemies)} offer: {string.Join(",", room.offer)}");

                // A simple legal stack: up to 3 offered parts, cheapest first, checked with the client rules.
                var stack = new List<string>();
                foreach (var id in room.offer.OrderBy(id => BobRules.Parts[id].Cost))
                    if (stack.Count < 3 && BobRules.WhyCannotPlace(stack, id, BobRules.RoundBudget - BobRules.StackCost(stack)) == null)
                        stack.Add(id);
                string stackJson = "[" + string.Join(",", stack.Select(id => "\"" + id + "\"")) + "]";

                var res = JsonUtility.FromJson<ScoreReply>(await server.ScoreStackAsync(seed, round, stackJson, BobRules.RoundBudget, 0.5, false));
                var local = BobRules.EvalStack(stack, room.enemies);
                Log($"stack {stackJson} -> server score {res.score.total} (pass={res.score.pass}), " +
                    $"server clean {res.stats.clean} vs client clean {local.Get("clean")}");
                Log(res.stats.clean == local.Get("clean") ? "SMOKE TEST PASSED" : "SMOKE TEST: client and server rules differ");
            }
            catch (Exception e)
            {
                Log("SMOKE TEST FAILED: " + e.Message + "  (is the server running? see README)");
            }
        }

        async void OnDestroy() { if (server != null) await server.CloseAsync(); }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));
            foreach (var l in lines) GUILayout.Label(l);
            GUILayout.EndArea();
        }

        // JsonUtility needs plain serializable classes that mirror the server's JSON.
        [Serializable] class Room { public int round; public string[] enemies; public string[] offer; }
        [Serializable] class Stats { public int clean, gentle, speed, shine; }
        [Serializable] class Score { public bool pass; public int total; }
        [Serializable] class ScoreReply { public Stats stats; public Score score; public int cost; }
    }
}
