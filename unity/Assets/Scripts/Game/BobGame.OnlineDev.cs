// Battle of the Brooms: the online bot test (menu Bob > Run Online Test). Start the bots first:
//   cd server && node mp-host.js 2 ../unity/Logs/bob-mp-match.txt
// They create a private match and write its id there; this bot joins it through the real online code, readies, then
// each round walks to a shelf, picks a part, carries it to the team cart, places it and submits.
// Success = Console "GAME TEST PASSED" with every round scored by the server and the team-mate drawn in the room.
using System.Collections;
using System.IO;
using System.Linq;
using Bob.Rules;
using UnityEngine;

namespace Bob.Game
{
    public partial class BobGame
    {
        IEnumerator RouteTo(Vector2 p, System.Func<bool> arrived)
        {
            float lane = pos.x < RoomW / 2 ? 2.6f : 5.4f; // lanes clear of stations, cart and sofa
            foreach (var w in new[] { new Vector2(lane, pos.y), new Vector2(lane, 3.3f), new Vector2(p.x, 3.3f), p })
            {
                target = w; stuck = 0;
                yield return WaitFor(() => Vector2.Distance(pos, w) < 0.08f || target == null, 8f);
            }
            yield return WaitFor(arrived, 2f);
        }

        IEnumerator AutoPlayOnline()
        {
            var file = Path.Combine(Application.dataPath, "../Logs/bob-mp-match.txt");
            Debug.Log("[BobOnlineBot] waiting for " + file + " (node mp-host.js writes it)");
            yield return WaitFor(() => !connecting && online, 15f);
            yield return WaitFor(() => File.Exists(file), 30f);
            if (!File.Exists(file) || !online) { Debug.Log("GAME TEST FAILED: no match file or server offline"); yield break; }
            var id = File.ReadAllText(file).Trim();
            var join = JoinOnlineAsync(id);
            yield return WaitFor(() => join.IsCompleted, 10f);
            if (join.IsFaulted || mp == null) { Debug.Log("GAME TEST FAILED: join " + join.Exception?.GetBaseException().Message); yield break; }
            yield return WaitFor(() => mp.State != null && mp.State.players.Length >= 2, 10f);
            yield return new WaitForSeconds(0.5f); Shot("online-1-lobby");
            ToggleReady();
            int scored = 0, sawMate = 0;
            for (int round = 1; round <= 3; round++)
            {
                yield return WaitFor(() => phase == Phase.Build && roundNo == round, 30f);
                if (phase != Phase.Build) { Debug.Log("GAME TEST FAILED: round " + round + " never started, phase " + phase); yield break; }
                var parts = R.Offer.Where(x => !BobRules.IsMethod(x)).ToArray();
                var mine = parts[(round * 3) % parts.Length];
                int si = (int)BobRules.Parts[mine].Family; var r = StationRects[si];
                var stand = new Vector2(r.center.x, si < 2 ? r.yMin - 0.3f : r.yMax + 0.3f);
                yield return RouteTo(stand, () => near == StationIds[si]);
                PickUp(mine);
                yield return WaitFor(() => hand.Count > 0, 3f);
                if (hand.Count == 0) Debug.LogWarning("[BobOnlineBot] the server did not give us " + mine);
                if (mates.Count > 0) sawMate++;
                if (round == 1) { Shot("online-2-build"); yield return new WaitForSeconds(0.2f); }
                yield return RouteTo(new Vector2(CartRect.xMin - 0.25f, CartRect.center.y), () => nearCart);
                int before = stack.Count;
                if (hand.Count > 0) Place(0);
                yield return WaitFor(() => stack.Count > before, 3f);
                yield return new WaitForSeconds(2.5f); // let the team-mate add theirs
                if (round == 1) { Shot("online-3-cart"); yield return new WaitForSeconds(0.2f); }
                Debug.Log($"[BobOnlineBot] round {round}: team cart {string.Join(",", stack)}, mates drawn {mates.Count}");
                Submit();
                yield return WaitFor(() => phase == Phase.Test, 40f);
                if (phase != Phase.Test) { Debug.Log("GAME TEST FAILED: no result for round " + round); yield break; }
                if (last.FromServer) scored++;
                Debug.Log($"[BobOnlineBot] round {round}: us {last.Mine.Total} vs them {last.Rival.Score.Total} -> {(last.Win ? "win" : "lose")} ({wins}:{losses})");
                if (round == 1) { yield return new WaitForSeconds(0.5f); Shot("online-4-test"); }
                if (MatchOver) break;
            }
            AfterTest();
            yield return WaitFor(() => phase == Phase.Result, 5f);
            yield return new WaitForSeconds(0.5f); Shot("online-5-result"); yield return new WaitForSeconds(0.3f);
            bool ok = phase == Phase.Result && scored == roundNo && sawMate > 0;
            Debug.Log(ok ? $"GAME TEST PASSED: online {wins}:{losses} in {roundNo} rounds, all scored by the server, team-mate drawn"
                         : $"GAME TEST FAILED: phase {phase}, server scored {scored}/{roundNo}, mate seen in {sawMate} rounds");
            LeaveOnline();
        }
    }
}
