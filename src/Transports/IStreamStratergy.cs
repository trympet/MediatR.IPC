using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace
#if MEDIATR
MediatR.IPC
#else
Mediator.IPC
#endif
{
    /// <summary>
    /// Represents a stream provider for IPC communication.
    /// Consumed by <see cref="IPCMediator.UseTransport(IStreamStratergy)"/>
    /// </summary>
    public interface IStreamStratergy
    {
        Task<Stream> Provide(StreamType type, string streamName, CancellationToken token);
    }

    /// <summary>
    /// Represents a stream provider for IPC communication.
    /// Consumed by <see cref="IPCMediator.UseTransport(IStreamStratergy)"/>
    /// </summary>
    public interface IStreamStratergy<TOptions> : IStreamStratergy
    {
        void WithOptions(TOptions options);
    }



    /// <summary>
    /// Specifies the type and direction of the stream.
    /// </summary>
    public enum StreamType
    {
        ClientStream,
        ServerStream,
    }
}
