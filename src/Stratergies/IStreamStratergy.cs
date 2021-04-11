using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.IPC
{
    public interface IStreamStratergy
    {
        internal Task<Stream> Provide(StreamType type, string streamName, CancellationToken token);
    }

    public enum StreamType
    {
        ClientStream,
        ServerStream,
    }
}
