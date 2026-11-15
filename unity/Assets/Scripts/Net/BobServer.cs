// Battle of the Brooms: thin wrapper over the Nakama client. Sign in with a device id, then call the game RPCs.
// The same code compiles in Unity (Nakama Unity SDK) and in plain .NET (NakamaClient NuGet, used by unity-check/).
using System.Threading.Tasks;
using Nakama;

namespace Bob.Net
{
    public sealed class BobServer
    {
        readonly IClient client;
        ISession session;

        // Defaults match server/docker-compose.yml. "defaultkey" is a development key: change it before any real deployment.
        public BobServer(string scheme = "http", string host = "127.0.0.1", int port = 7350, string serverKey = "defaultkey")
        {
#if UNITY_5_3_OR_NEWER
            client = new Client(scheme, host, port, serverKey, UnityWebRequestAdapter.Instance);
#else
            client = new Client(scheme, host, port, serverKey);
#endif
        }

        ISocket socket;

        /// <summary>
        /// Builds the client from the Settings field: "host", "host:port" or "https://host[:port]" (443 when the scheme
        /// says https). A public server behind Caddy/Cloudflare Tunnel needs the https form.
        /// </summary>
        public static BobServer FromAddress(string address, int defaultPort = 7350, string serverKey = "defaultkey")
        {
            var s = (address ?? "").Trim();
            var scheme = "http";
            var port = defaultPort;
            if (s.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase)) { scheme = "https"; s = s.Substring(8); port = 443; }
            else if (s.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase)) { s = s.Substring(7); }
            var slash = s.IndexOf('/'); if (slash >= 0) s = s.Substring(0, slash);
            var colon = s.LastIndexOf(':');
            if (colon > 0 && int.TryParse(s.Substring(colon + 1), out var p)) { port = p; s = s.Substring(0, colon); }
            if (s.Length == 0) s = "127.0.0.1";
            return new BobServer(scheme, s, port, serverKey);
        }

        public bool SignedIn => session != null && !session.IsExpired;
        public string UserId => session?.UserId;

        public async Task SignInAsync(string deviceId) => session = await client.AuthenticateDeviceAsync(deviceId);

        async Task<string> Rpc(string id, string json) => (await client.RpcAsync(session, id, json)).Payload;

        /// <summary>Opens the realtime socket (needed for matches). Call after SignInAsync.</summary>
        public async Task ConnectAsync()
        {
#if UNITY_5_3_OR_NEWER
            socket = client.NewSocket(useMainThread: true); // callbacks arrive on Unity's main thread
#else
            socket = Socket.From(client, new WebSocketStdlibAdapter()); // System.Net.WebSockets; works without IPv6
#endif
            await socket.ConnectAsync(session, appearOnline: true);
        }

        /// <summary>The realtime socket (after ConnectAsync). BobOnline runs the multiplayer match over it.</summary>
        public ISocket Realtime => socket;

        /// <summary>Creates a relayed Nakama match. For the real game use BobOnline (server-authoritative 'bob_coop').</summary>
        public Task<IMatch> CreateMatchAsync() => socket.CreateMatchAsync();

        /// <summary>Creates a private server-authoritative 'bob_coop' match. Reply: {"match_id":"..."}. seed 0 = random room.</summary>
        public Task<string> CreateMatchRpcAsync(int seed = 0) => Rpc("bob_create_match", seed > 0 ? $"{{\"seed\":{seed}}}" : "{}");

        /// <summary>
        /// A Task Race room (docs 16): the same 'bob_coop' match with mode = taskrace, so match.js deals the seeded
        /// list and validates opcodes 16-18. difficulty is the chapter band 1-3. seed 0 = random.
        /// </summary>
        public Task<string> CreateTaskRaceMatchAsync(int seed = 0, int difficulty = 1) =>
            Rpc("bob_create_match", $"{{\"mode\":\"taskrace\",\"difficulty\":{difficulty}" + (seed > 0 ? $",\"seed\":{seed}" : "") + "}");

        public Task<IMatch> JoinMatchAsync(string matchId) => socket.JoinMatchAsync(matchId);

        /// <summary>Opens the realtime socket unless it is already open.</summary>
        public Task EnsureConnectedAsync() => socket != null && socket.IsConnected ? Task.CompletedTask : ConnectAsync();

        public Task CloseAsync() => socket != null ? socket.CloseAsync() : Task.CompletedTask;

        /// <summary>Stable room seed from a match id, so every player in the match gets the same room.</summary>
        public static int SeedFromMatchId(string matchId)
        {
            unchecked
            {
                int h = (int)2166136261;
                foreach (char c in matchId) h = (h ^ c) * 16777619;
                return 1 + (h & 0x7FFFFF); // positive and small enough for the JS rules' seed math
            }
        }

        public Task<string> PingAsync() => Rpc("bob_ping", "{}");

        /// <summary>The room for this round. Both teams get the same room for the same seed.</summary>
        public Task<string> MakeRoundAsync(int seed, int round) => Rpc("bob_make_round", $"{{\"seed\":{seed},\"round\":{round}}}");

        /// <summary>The authoritative score for a stack. stackJson is a JSON array of part ids, bottom layer first.</summary>
        /// <param name="match">Optional analytics label: 1-64 letters, digits, _ or -.</param>
        /// <param name="character">Optional analytics label: sweep, bubble, spray or press.</param>
        public Task<string> ScoreStackAsync(int seed, int round, string stackJson, int budget, double timeFrac, bool shield,
            string match = null, string character = null) =>
            Rpc("bob_score_stack", $"{{\"seed\":{seed},\"round\":{round},\"stack\":{stackJson},\"budget\":{budget}," +
                $"\"timeFrac\":{timeFrac.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"shield\":{(shield ? "true" : "false")}" +
                (match != null ? $",\"match\":\"{match}\"" : "") + (character != null ? $",\"char\":\"{character}\"" : "") + "}");

        /// <summary>Client analytics event: match_start, round_result, ability_used or match_end. dataJson is a JSON object.</summary>
        public Task<string> LogEventAsync(string evt, string match, string dataJson) =>
            Rpc("bob_log_event", $"{{\"event\":\"{evt}\",\"match\":\"{match}\",\"data\":{dataJson}}}");
    }
}
