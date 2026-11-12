// Battle of the Brooms: a bot that plays the Coop scene through the real game code, for Bob > Run Coop Test.
// Start the other players first:  cd server && node mp-host.js 2 ../unity/Logs/bob-mp-match.txt
// The bot joins that private room, readies, then each round walks to a shelf, picks a part, carries it to the team cart,
// places it, votes to send, and checks the round result arrives. Screenshots go to <project>/Logs/bob-coop-*.png.
// Success = Console "COOP TEST PASSED".
using System.Collections;
using System.IO;
using System.Linq;
using Bob.Net;
using Bob.Rules;
using UnityEngine;

namespace Bob.Coop
{
    public partial class BobCoopGame
    {
        public const string AutoKey = "Bob.AutoCoop";

        bool AutoRequested()
        {
#if UNITY_EDITOR
            return UnityEditor.SessionState.GetBool(AutoKey, false);
#else
            return false;
#endif
        }

        void CoopShot(string name)
        {
            var dir = Path.Combine(Application.dataPath, "../Logs"); Directory.CreateDirectory(dir);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, "bob-coop-" + name + ".png"));
        }

        IEnumerator CoopWait(System.Func<bool> cond, float timeout)
        {
            float t = 0; while (!cond() && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
        }

        // walk by sending positions the way the phone does; the server moves us and sends POS back
        IEnumerator CoopWalk(Vector2 to)
        {
            for (int k = 0; k < 80; k++)
            {
                var me = net.MePlayer; var d = to - new Vector2(me.x, me.y);
                if (d.magnitude < 0.06f) yield break;
                var step = Vector2.ClampMagnitude(d, 0.25f);
                net.Move(me.x + step.x, me.y + step.y);
                yield return new WaitForSeconds(0.1f);
            }
        }

        IEnumerator CoopRoute(Vector2 to)
        {
            var me = net.MePlayer; float lane = me.x < 4f ? 2.6f : 5.4f;
            foreach (var w in new[] { new Vector2(lane, me.y), new Vector2(lane, 3.3f), new Vector2(to.x, 3.3f), to }) yield return CoopWalk(w);
        }

        static readonly System.Collections.Generic.Dictionary<string, Vector2> Shelf = new System.Collections.Generic.Dictionary<string, Vector2>
        { { "tool", new Vector2(1.2f, 3.75f) }, { "chem", new Vector2(6.8f, 3.75f) }, { "energy", new Vector2(1.2f, 1.25f) } };

        IEnumerator AutoPlayCoop()
        {
            var file = Path.Combine(Application.dataPath, "../Logs/bob-mp-match.txt");
            Debug.Log("[BobCoopBot] waiting for the server and " + file);
            yield return CoopWait(() => screen == View.Menu, 20f);
            if (screen != View.Menu) { Debug.Log("COOP TEST FAILED: no server (view " + screen + ")"); yield break; }
            CoopShot("1-menu");
            yield return CoopWait(() => File.Exists(file), 30f);
            if (!File.Exists(file)) { Debug.Log("COOP TEST FAILED: no match file"); yield break; }
            JoinRoom(File.ReadAllText(file).Trim());
            yield return CoopWait(() => net != null && net.State != null && net.State.players.Length >= 2, 10f);
            if (net == null) { Debug.Log("COOP TEST FAILED: could not join"); yield break; }
            yield return new WaitForSeconds(0.6f); CoopShot("2-lobby");
            ToggleReady();
            int placed = 0, rounds = 0, sawMate = 0;
            for (int round = 1; round <= 3; round++)
            {
                yield return CoopWait(() => net.State.phase == "build" && net.State.round == round, 30f);
                if (net.State.phase != "build") { Debug.Log("COOP TEST FAILED: round " + round + " never started, phase " + net.State.phase); yield break; }
                var s = net.State; int my = net.MyTeam;
                var offer = s.room.offer.Where(x => !BobRules.IsMethod(x)).ToArray();
                var part = offer[(round * 3) % offer.Length];
                var fam = BobRules.Parts[part].Family.ToString().ToLower();
                if (!Shelf.TryGetValue(fam, out var spot)) spot = Shelf["tool"];
                yield return CoopRoute(spot);
                net.Pick(part);
                yield return CoopWait(() => net.MePlayer.hand != null && net.MePlayer.hand.Length > 0, 3f);
                if (round == 1) { yield return new WaitForSeconds(0.3f); CoopShot("3-build"); }
                if (net.State.players.Count(p => p.team == my) > 1) sawMate++;
                var cart = net.State.teams[my];
                yield return CoopRoute(new Vector2(cart.cx - 0.9f, cart.cy));
                int before = net.State.teams[my].stack.Length;
                if (net.MePlayer.hand != null && net.MePlayer.hand.Length > 0) net.Place(0);
                yield return CoopWait(() => net.State.teams[my].stack.Length > before, 3f);
                if (net.State.teams[my].stack.Length > before) placed++;
                net.Ability(); // the hero's ability, once per round (Sweep needs 2 layers; a refusal is fine)
                yield return new WaitForSeconds(2.5f);
                if (round == 1)
                {
                    // a screenshot is taken at the end of the frame, so wait before changing the screen again
                    CoopShot("4-cart"); yield return new WaitForSeconds(0.4f);
                    SetOv(Ov.Pause); yield return new WaitForSeconds(0.4f); CoopShot("4b-pause"); yield return new WaitForSeconds(0.4f);
                    SetOv(Ov.Confirm); yield return new WaitForSeconds(0.4f); CoopShot("4c-confirm"); yield return new WaitForSeconds(0.4f);
                    SetOv(Ov.None); yield return new WaitForSeconds(0.2f);
                }
                net.Submit();
                yield return CoopWait(() => net.LastResult != null && net.LastResult.round >= round, 40f);
                if (net.LastResult == null || net.LastResult.round < round) { Debug.Log("COOP TEST FAILED: no result for round " + round); yield break; }
                rounds++;
                Debug.Log($"[BobCoopBot] round {round}: {string.Join(" vs ", net.LastResult.teams.Select(t => string.Join(",", t.stack) + "=" + t.score.total))} winner {net.LastResult.winner}");
                if (round == 1) { yield return new WaitForSeconds(0.5f); CoopShot("5-test"); }
                if (net.LastResult.over) break;
            }
            yield return CoopWait(() => screen == View.Result, 20f);
            yield return new WaitForSeconds(0.4f); CoopShot("6-result");
            bool ok = screen == View.Result && rounds >= 2 && placed >= rounds && sawMate > 0;
            Debug.Log(ok ? $"COOP TEST PASSED: {rounds} rounds, {placed} layers placed, team-mate seen"
                         : $"COOP TEST FAILED: view {screen}, rounds {rounds}, placed {placed}, mate seen {sawMate}");
        }
    }
}
