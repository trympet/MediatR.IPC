using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.IPC
{
    public partial class IPCTransport
    {
        private class NamedPipeStratergy : IStreamStratergy
        {
            async Task<Stream> IStreamStratergy.Provide(StreamType type, string streamName, CancellationToken token)
            {
                if (type == StreamType.ClientStream)
                {
                    var pipe = new NamedPipeClientStream(".", streamName, PipeDirection.InOut, PipeOptions.Asynchronous);
                    await pipe.ConnectAsync(token).ConfigureAwait(false);
                    return pipe;
                }
                else
                {
                    var pipe = new NamedPipeServerStream(streamName, PipeDirection.InOut, 10, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    await pipe.WaitForConnectionAsync(token).ConfigureAwait(false);
                    return pipe;
                }
            }
        }
    }
}
