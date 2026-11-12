// Battle of the Brooms: client side of the server-authoritative match 'bob_coop' (server/match.js).
// The server owns everything: positions, hands, each team's shared cart, the clock, rounds and scores.
// This class only sends intents and raises events with what the server says; the scene draws from State.
// Flow: BobServer.SignInAsync -> ConnectAsync -> BobOnline.FindAsync / CreateAsync / JoinAsync
//       -> Ready(char) in the lobby -> (build) Move / Pick / Place / Lift / Submit -> OnResult each round.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nakama;
using Nakama.TinyJson;

namespace Bob.Net
{
    // ---- wire format (field names match server/match.js exactly) ----
    public sealed class MpNeed { public int clean = -1, gentle = -1, speed = -1, shine = -1; }
    public sealed class MpSpec { public MpNeed need; public string[] notes; }
    public sealed class MpRoom { public int round; public string[] enemies; public string[] offer; public MpSpec spec; }
    public sealed class MpPlayer { public string id, name, @char; public int team; public bool ready, online, abUsed; public float x, y; public string[] hand; }
    /// <summary>time: this team's own clock (Press freezes it for `freeze` s). shield: Bubble's shield is on.</summary>
    public sealed class MpTeam { public string[] stack; public int budget, wins; public bool submitted, shield; public float time, freeze; }
    /// <summary>phase: lobby, build, test (between rounds), result (match over).</summary>
    public sealed class MpState { public string phase; public int seed, round; public float time; public MpRoom room; public MpPlayer[] players; public MpTeam[] teams; }
    public sealed class MpPos { public string id; public float x, y; }
    public sealed class MpPositions { public float time; public float[] tt; public MpPos[] p; }
    public sealed class MpPreview { public int total; public bool pass; }
    public sealed class MpStats { public int clean, gentle, speed, shine; }
    public sealed class MpScore { public bool pass; public double test, budget, time; public int total; }
    public sealed class MpEffect { public int[] d; public string t, src; public bool good, off; }
    public sealed class MpTeamResult { public int team, cost, left; public string[] stack; public MpStats stats; public MpScore score; public MpEffect[] effects; }
    /// <summary>winner: team 0 or 1, or -1 for a draw. over: the match has ended (best of 3).</summary>
    public sealed class MpResult { public int round, winner; public MpTeamResult[] teams; public int[] wins; public bool over; }
    sealed class MpError { public string t; }

    public sealed class BobOnline
    {
        public const long OpMove = 1, OpPick = 2, OpPlace = 3, OpLift = 4, OpReady = 5, OpSubmit = 6, OpAbility = 7;
        public const long OpState = 10, OpPos = 11, OpResult = 12, OpError = 13, OpPreview = 14;
        public const string MatchName = "bob_coop";

        readonly ISocket socket;
        public string MatchId { get; private set; }
        public string Me { get; }
        public MpState State { get; private set; }
        public MpResult LastResult { get; private set; }

        /// <summary>Full snapshot: lobby changes, picks, places, phase changes.</summary>
        public event Action<MpState> OnState;
        /// <summary>Every server tick (10/s) during build: all positions and the clock.</summary>
        public event Action<MpPositions> OnPositions;
        public event Action<MpResult> OnResult;
        /// <summary>The server refused an intent; the text is Thai and ready to show as a toast.</summary>
        public event Action<string> OnRefused;
        /// <summary>Spray's ability: what the team cart would score now (sent only to that player).</summary>
        public event Action<MpPreview> OnPreview;

        BobOnline(ISocket socket, string me)
        {
            this.socket = socket; Me = me;
            socket.ReceivedMatchState += Receive;
        }

        public MpPlayer MePlayer => State?.players?.FirstOrDefault(p => p.id == Me);
        public int MyTeam => MePlayer?.team ?? 0;
        public MpTeam MyCart => State?.teams?[MyTeam];

        // ---------------------------------------------------------------- joining

        /// <summary>Quick play: queue in the matchmaker for 2-4 players; the server starts one match for the group.</summary>
        public static async Task<BobOnline> FindAsync(BobServer server, int minPlayers = 2, int maxPlayers = 4)
        {
            var o = new BobOnline(server.Realtime, server.UserId);
            var found = new TaskCompletionSource<IMatchmakerMatched>();
            finding = found;
            void Matched(IMatchmakerMatched m) => found.TrySetResult(m);
            server.Realtime.ReceivedMatchmakerMatched += Matched;
            try
            {
                ticket = await server.Realtime.AddMatchmakerAsync("*", minPlayers, maxPlayers);
                var m = await found.Task; // throws TaskCanceledException after CancelFindAsync
                await o.JoinById(m.MatchId);
            }
            finally { server.Realtime.ReceivedMatchmakerMatched -= Matched; ticket = null; finding = null; }
            return o;
        }

        static IMatchmakerTicket ticket;
        static TaskCompletionSource<IMatchmakerMatched> finding;

        /// <summary>Stops a FindAsync that is still waiting (its task then throws TaskCanceledException).</summary>
        public static async Task CancelFindAsync(BobServer server)
        {
            var t = ticket; finding?.TrySetCanceled();
            if (t != null) try { await server.Realtime.RemoveMatchmakerAsync(t); } catch (Exception) { }
        }

        /// <summary>Private room: creates a match on the server; share MatchId so friends can JoinAsync it.</summary>
        public static async Task<BobOnline> CreateAsync(BobServer server, int seed = 0)
        {
            var reply = (await server.CreateMatchRpcAsync(seed)).FromJson<Dictionary<string, object>>();
            return await JoinAsync(server, (string)reply["match_id"]);
        }

        public static async Task<BobOnline> JoinAsync(BobServer server, string matchId)
        {
            var o = new BobOnline(server.Realtime, server.UserId);
            await o.JoinById(matchId);
            return o;
        }

        async Task JoinById(string id) { var m = await socket.JoinMatchAsync(id); MatchId = m.Id; }

        public async Task LeaveAsync()
        {
            socket.ReceivedMatchState -= Receive;
            if (MatchId != null) try { await socket.LeaveMatchAsync(MatchId); } catch (Exception) { }
            MatchId = null;
        }

        // ---------------------------------------------------------------- intents (fire and forget; the server answers with STATE or ERROR)

        public void Ready(string character, bool ready = true) => Send(OpReady, "{\"char\":\"" + character + "\",\"ready\":" + (ready ? "true" : "false") + "}");
        /// <summary>Send your position about 10 times a second while walking. The server caps speed and keeps you in the room.</summary>
        public void Move(float x, float y) => Send(OpMove, "{\"x\":" + F(x) + ",\"y\":" + F(y) + "}");
        public void Pick(string partId) => Send(OpPick, "{\"id\":\"" + partId + "\"}");
        public void Place(int handIndex) => Send(OpPlace, "{\"i\":" + handIndex + "}");
        public void Lift() => Send(OpLift, "{}");
        /// <summary>Locks your team's cart for this round. The round is scored when both teams submit or time runs out.</summary>
        public void Submit() => Send(OpSubmit, "{}");
        /// <summary>Your hero's ability, once per round. layer = Sweep's lower layer to swap with the one above; ignored by the others.</summary>
        public void Ability(int layer = 0) => Send(OpAbility, "{\"i\":" + layer + "}");

        static string F(float v) => v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

        void Send(long op, string json)
        {
            if (MatchId == null) return;
            _ = socket.SendMatchStateAsync(MatchId, op, json);
        }

        // ---------------------------------------------------------------- receiving

        void Receive(IMatchState m)
        {
            if (m.MatchId != MatchId) return;
            var json = Encoding.UTF8.GetString(m.State);
            try
            {
                switch (m.OpCode)
                {
                    case OpState: State = json.FromJson<MpState>(); OnState?.Invoke(State); break;
                    case OpPos:
                        var p = json.FromJson<MpPositions>();
                        if (State?.players != null)
                        {
                            State.time = p.time;
                            foreach (var q in p.p) { var pl = State.players.FirstOrDefault(x => x.id == q.id); if (pl != null) { pl.x = q.x; pl.y = q.y; } }
                        }
                        OnPositions?.Invoke(p); break;
                    case OpResult: LastResult = json.FromJson<MpResult>(); OnResult?.Invoke(LastResult); break;
                    case OpError: OnRefused?.Invoke(json.FromJson<MpError>().t); break;
                    case OpPreview: OnPreview?.Invoke(json.FromJson<MpPreview>()); break;
                }
            }
            catch (Exception e)
            {
#if UNITY_5_3_OR_NEWER
                UnityEngine.Debug.LogWarning("[BobOnline] bad message op " + m.OpCode + ": " + e.Message);
#endif
            }
        }
    }
}
