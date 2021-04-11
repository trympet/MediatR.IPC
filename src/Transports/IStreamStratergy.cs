using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.IPC
{
    public interface IStreamStratergy
    {
        Task<Stream> Provide(StreamType type, string streamName, CancellationToken token);
    }

    public enum StreamType
    {
        ClientStream,
        ServerStream,
    }
}
