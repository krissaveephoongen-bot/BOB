// Stand-ins for the few Unity-only Nakama helpers (from the nakama-unity package) that the plain NakamaClient NuGet lacks.
// Only used by this type-check project; never copied into Unity.
namespace Nakama
{
    public sealed class UnityWebRequestAdapter : IHttpAdapter
    {
        public static UnityWebRequestAdapter Instance { get; } = new UnityWebRequestAdapter();
        readonly IHttpAdapter inner = HttpRequestAdapter.WithGzip();
        public ILogger Logger { get => inner.Logger; set => inner.Logger = value; }
        public TransientExceptionDelegate TransientExceptionDelegate => inner.TransientExceptionDelegate;
        public System.Threading.Tasks.Task<string> SendAsync(string method, System.Uri uri, System.Collections.Generic.IDictionary<string, string> headers,
            byte[] body, int timeoutSec, System.Threading.CancellationToken? cancellationToken) => inner.SendAsync(method, uri, headers, body, timeoutSec, cancellationToken);
    }
    public static class ClientUnityExtensions
    {
        public static ISocket NewSocket(this IClient client, bool useMainThread = false) => Socket.From(client);
    }
}
