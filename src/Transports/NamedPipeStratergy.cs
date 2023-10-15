using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace
#if MEDIATR
MediatR.IPC
#else
Mediator.IPC
#endif
{
    public partial class IPCTransport
    {
        private class NamedPipeStratergy : IStreamStratergy
        {
            async Task<Stream> IStreamStratergy.Provide(StreamType type, string streamName, CancellationToken token)
            {
                Stream result;
                if (type == StreamType.ClientStream)
                {
                    var pipe = new NamedPipeClientStream(".", streamName, PipeDirection.InOut, PipeOptions.Asynchronous);
                    await pipe.ConnectAsync(token).ConfigureAwait(false);
                    result = pipe;
                }
                else
                {
                    var pipe = new NamedPipeServerStream(streamName, PipeDirection.InOut, 10, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    try
                    {
                        await pipe.WaitForConnectionAsync(token).ConfigureAwait(false);
                        token.ThrowIfCancellationRequested();
                    }
                    catch (Exception)
                    {
                        pipe.Close();
                        pipe.Dispose();
                        throw;
                    }

                    result = pipe;
                }

                token.Register(static x => ((Stream)x!).Dispose(), result);
                return result;
            }
        }
    }
}
