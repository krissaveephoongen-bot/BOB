// Battle of the Brooms: client side of the server-authoritative match 'bob_coop' (server/match.js).
// The server owns everything: positions, hands, each team's shared cart, the clock, rounds and scores.
// This class only sends intents and raises events with what the server says; the scene draws from State.
// Flow: BobServer.SignInAsync -> ConnectAsync -> BobOnline.FindAsync / CreateAsync / JoinAsync
//       -> Ready(char) in the lobby -> (build) Move / Pick / Place / Lift / Push / Pass / TablePut / TableTake / Submit -> OnResult each round.
// Each team plays in its own copy of the room (same coordinates); the layout is in Coop/BobCoopLayout.cs.
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
    /// <summary>vote: this player voted to send the cart this round.</summary>
    public sealed class MpPlayer { public string id, name, @char; public int team; public bool ready, online, vote, ab; public float x, y; public string[] hand; }
    /// <summary>cx, cy: the team's cart (it can be pushed); pusher: user id pushing it, "" = nobody; table: parts parked on the team's table.</summary>
    public sealed class MpTeam { public string[] stack; public int budget, wins; public bool submitted; public float cx, cy; public string pusher; public string[] table;
        public float time, freeze; public bool shield; } // time: the team's own clock (Press stops it for `freeze` s); shield: Bubble's
    /// <summary>phase: lobby, build, test (between rounds), result (match over).</summary>
    public sealed class MpState { public string phase; public int seed, round; public float time; public MpRoom room; public MpPlayer[] players; public MpTeam[] teams; }
    public sealed class MpPos { public string id; public float x, y; }
    public sealed class MpCartPos { public float x, y; }
    /// <summary>c: each team's cart, by team index.</summary>
    public sealed class MpPositions { public float time; public float[] tt; public MpPos[] p; public MpCartPos[] c; }
    public sealed class MpStats { public int clean, gentle, speed, shine; }
    public sealed class MpScore { public bool pass; public double test, budget, time; public int total; }
    public sealed class MpEffect { public int[] d; public string t, src; public bool good, off; }
    public sealed class MpTeamResult { public int team, cost, left; public string[] stack; public MpStats stats; public MpScore score; public MpEffect[] effects; }
    /// <summary>winner: team 0 or 1, or -1 for a draw. over: the match has ended (best of 3).</summary>
    public sealed class MpResult { public int round, winner; public MpTeamResult[] teams; public int[] wins; public bool over; }
    sealed class MpError { public string t; }
    [Serializable] public sealed class MpPeek { public int total; public MpStats stats; } // Spray's private preview

    public sealed class BobOnline
    {
        public const long OpMove = 1, OpPick = 2, OpPlace = 3, OpLift = 4, OpReady = 5, OpSubmit = 6, OpPush = 7, OpPass = 8, OpTable = 9, OpAbility = 14;
        public const long OpState = 10, OpPos = 11, OpResult = 12, OpError = 13, OpPeek = 15;
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
        /// <summary>Spray's ability: what the team's cart would score now (only this player gets it).</summary>
        public event Action<MpPeek> OnPeek;

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

        // MatchId is set before the join answer arrives: the server broadcasts the lobby STATE for our own join first,
        // and Receive() drops messages for any other match id (the last player to join used to see an empty lobby).
        async Task JoinById(string id) { MatchId = id; var m = await socket.JoinMatchAsync(id); MatchId = m.Id; SavedMatchId = m.Id; }

        // ---------------------------------------------------------------- reconnect after the app was closed (GDD 10)
        // The match id is kept on the phone until the match ends or the player leaves on purpose. The server keeps a
        // dropped player's seat (same device id = same user), so joining the same id again puts them back in their team.
#if UNITY_5_3_OR_NEWER
        public static string SavedMatchId
        {
            get { var v = UnityEngine.PlayerPrefs.GetString("bob.match", ""); return v.Length > 0 ? v : null; }
            set { if (value == null) UnityEngine.PlayerPrefs.DeleteKey("bob.match"); else UnityEngine.PlayerPrefs.SetString("bob.match", value); UnityEngine.PlayerPrefs.Save(); }
        }
#else
        public static string SavedMatchId { get; set; }
#endif
        /// <summary>Back into the match this phone was in; null when there is none or it has ended.</summary>
        public static async Task<BobOnline> RejoinAsync(BobServer server)
        {
            var id = SavedMatchId; if (id == null) return null;
            try { return await JoinAsync(server, id); }
            catch (Exception) { SavedMatchId = null; return null; }
        }

        public async Task LeaveAsync()
        {
            socket.ReceivedMatchState -= Receive;
            if (MatchId != null) try { await socket.LeaveMatchAsync(MatchId); } catch (Exception) { }
            MatchId = null; SavedMatchId = null;
        }

        // ---------------------------------------------------------------- intents (fire and forget; the server answers with STATE or ERROR)

        public void Ready(string character, bool ready = true) => Send(OpReady, "{\"char\":\"" + character + "\",\"ready\":" + (ready ? "true" : "false") + "}");
        /// <summary>Lobby only: move to team 0 or 1 (refused when that side is full).</summary>
        public void Ready(string character, bool ready, int team) =>
            Send(OpReady, "{\"char\":\"" + character + "\",\"ready\":" + (ready ? "true" : "false") + ",\"team\":" + team + "}");
        /// <summary>Send your position about 10 times a second while walking. The server caps speed and keeps you in the room.</summary>
        public void Move(float x, float y) => Send(OpMove, "{\"x\":" + F(x) + ",\"y\":" + F(y) + "}");
        public void Pick(string partId) => Send(OpPick, "{\"id\":\"" + partId + "\"}");
        public void Place(int handIndex) => Send(OpPlace, "{\"i\":" + handIndex + "}");
        public void Lift() => Send(OpLift, "{}");
        /// <summary>Your vote to send the team's cart; it is sent when half the online team has voted.
        /// The round is scored when both teams have sent or time runs out.</summary>
        public void Submit(bool vote = true) => Send(OpSubmit, vote ? "{}" : "{\"vote\":false}");
        /// <summary>Grab (true) or let go of (false) your team's cart. While grabbed it moves with you, a little slower.</summary>
        public void Push(bool on) => Send(OpPush, on ? "{\"on\":true}" : "{\"on\":false}");
        /// <summary>Hand a part to a team-mate within 1 unit; toUserId null = the nearest one.</summary>
        public void Pass(int handIndex, string toUserId = null) =>
            Send(OpPass, "{\"i\":" + handIndex + (string.IsNullOrEmpty(toUserId) ? "" : ",\"to\":\"" + toUserId + "\"") + "}");
        /// <summary>Park a part from your hand on the team's table (stand next to it).</summary>
        public void TablePut(int handIndex) => Send(OpTable, "{\"put\":" + handIndex + "}");
        /// <summary>Take a part from the team's table into your hand.</summary>
        public void TableTake(int tableIndex) => Send(OpTable, "{\"take\":" + tableIndex + "}");
        /// <summary>Use the hero's ability (once a round). Sweep passes the layer to swap with the one above it.</summary>
        public void Ability(int swapLayer = -1) => Send(OpAbility, swapLayer >= 0 ? "{\"swap\":" + swapLayer + "}" : "{}");

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
                            if (p.c != null && State.teams != null)
                                for (int i = 0; i < p.c.Length && i < State.teams.Length; i++) { State.teams[i].cx = p.c[i].x; State.teams[i].cy = p.c[i].y; }
                            if (p.tt != null && State.teams != null)
                                for (int i = 0; i < p.tt.Length && i < State.teams.Length; i++) State.teams[i].time = p.tt[i];
                        }
                        OnPositions?.Invoke(p); break;
                    case OpResult: LastResult = json.FromJson<MpResult>(); if (LastResult.over) SavedMatchId = null; OnResult?.Invoke(LastResult); break;
                    case OpPeek: OnPeek?.Invoke(json.FromJson<MpPeek>()); break;
                    case OpError: OnRefused?.Invoke(json.FromJson<MpError>().t); break;
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
