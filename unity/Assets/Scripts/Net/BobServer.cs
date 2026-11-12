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

        /// <summary>Creates a new Nakama match (relayed for now; a server-authoritative match handler comes later).</summary>
        public Task<IMatch> CreateMatchAsync() => socket.CreateMatchAsync();

        public Task<IMatch> JoinMatchAsync(string matchId) => socket.JoinMatchAsync(matchId);

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
        public Task<string> ScoreStackAsync(int seed, int round, string stackJson, int budget, double timeFrac, bool shield) =>
            Rpc("bob_score_stack", $"{{\"seed\":{seed},\"round\":{round},\"stack\":{stackJson},\"budget\":{budget}," +
                $"\"timeFrac\":{timeFrac.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"shield\":{(shield ? "true" : "false")}}}");
    }
}
