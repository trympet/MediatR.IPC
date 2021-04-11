using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.IPC
{
    public partial class IPCTransport
    {
        public static readonly IStreamStratergy NamedPipe = new NamedPipeStratergy();

#if !NETSTANDARD
        [System.Runtime.Versioning.SupportedOSPlatform("Unix")]
#endif
        public static readonly IStreamStratergy UnixDomainSocket = new UnixDomainSocketStratergy();

        public static readonly IStreamStratergy Default = new DefaultStreamStratergy();

        private class DefaultStreamStratergy : IStreamStratergy
        {
            Task<Stream> IStreamStratergy.Provide(StreamType type, string streamName, CancellationToken token)
            {
                return NamedPipe.Provide(type, streamName, token);
            }
        }
    }
}
