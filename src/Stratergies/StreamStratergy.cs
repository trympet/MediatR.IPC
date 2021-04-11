using MediatR.IPC;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.IPC
{
    public class IPCTransport
    {
        public static readonly IStreamStratergy NamedPipe = new NamedPipeStratergyImpl();

#if !NETSTANDARD
        [System.Runtime.Versioning.SupportedOSPlatform("Unix")]
#endif
        public static readonly IStreamStratergy UnixDomainSocket = new UnixDomainSocketStratergyImpl();

        public static readonly IStreamStratergy Default = new DefaultStreamStratergy();

        private class DefaultStreamStratergy : IStreamStratergy
        {
            Task<Stream> IStreamStratergy.Provide(StreamType type, string streamName, CancellationToken token)
            {
                return NamedPipe.Provide(type, streamName, token);
            }
        }

        private class NamedPipeStratergyImpl : IStreamStratergy
        {
            async Task<Stream> IStreamStratergy.Provide(StreamType type, string streamName, CancellationToken token)
            {
                if (type == StreamType.ClientStream)
                {
                    var pipe = new NamedPipeClientStream(".", streamName, PipeDirection.InOut, PipeOptions.Asynchronous);
                    token.Register(() => pipe.Close());
                    await pipe.ConnectAsync(token).ConfigureAwait(false);
                    return pipe;
                }
                else
                {
                    var pipe = new NamedPipeServerStream(streamName, PipeDirection.InOut, 10, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    token.Register(() => pipe.Close());
                    await pipe.WaitForConnectionAsync(token).ConfigureAwait(false);
                    return pipe;
                }
            }
        }

        private class UnixDomainSocketStratergyImpl : IStreamStratergy
        {
            private readonly ConcurrentDictionary<string, bool> assertedSockets = new();

            async Task<Stream> IStreamStratergy.Provide(StreamType type, string streamName, CancellationToken token)
            {
                if (type == StreamType.ClientStream)
                {
                    var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                    bool connected = false;
                    while (!connected)
                    {
                        try
                        {
                            await socket.ConnectAsync(new UnixDomainSocketEndPoint(streamName));
                            connected = true;
                        }
                        catch (SocketException ex)
                        {
                            await Task.Delay(500);
                        }
                    }
                    return new NetworkStream(socket, FileAccess.ReadWrite, true);
                }
                else
                {
                    if (File.Exists(streamName))
                    {
                        assertedSockets[streamName] = true;
                        File.Delete(streamName);
                    }
                    var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                    socket.Bind(new UnixDomainSocketEndPoint(streamName));
                    socket.Listen(1);
                    var connectedSocket = await socket.AcceptAsync();
                    return new NetworkStream(connectedSocket, FileAccess.ReadWrite, true);

                    //var buffer = new byte[4096];
                    //var memory = new Memory<byte>(buffer);
                    //var bytesReceived = await x.ReceiveAsync(memory, SocketFlags.None);


                    //return new MemoryStream(memory.Slice(0, bytesReceived).ToArray());
                }
            }
        }
    }
}
