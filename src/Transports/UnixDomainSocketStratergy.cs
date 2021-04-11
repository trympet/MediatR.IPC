using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.IPC
{
    public partial class IPCTransport
    {
        private class UnixDomainSocketStratergy : IStreamStratergy
        {
            async Task<Stream> IStreamStratergy.Provide(StreamType type, string streamName, CancellationToken token)
            {
                if (type == StreamType.ClientStream)
                {
                    var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                    await Connect(streamName, socket).ConfigureAwait(false);
                    return new NetworkStream(socket, FileAccess.ReadWrite, true);
                }
                else
                {
                    if (File.Exists(streamName))
                    {
                        File.Delete(streamName);
                    }

                    var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                    socket.Bind(new UnixDomainSocketEndPoint(streamName));
                    socket.Listen(1);
                    var connectedSocket = await socket.AcceptAsync().ConfigureAwait(false);
                    return new NetworkStream(connectedSocket, FileAccess.ReadWrite, true);
                }
            }

            private static async Task Connect(string streamName, Socket socket)
            {
                bool connected = false;
                while (!connected)
                {
                    try
                    {
                        await socket.ConnectAsync(new UnixDomainSocketEndPoint(streamName)).ConfigureAwait(false);
                        connected = true;
                    }
                    catch (SocketException)
                    {
                        await Task.Delay(500).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
