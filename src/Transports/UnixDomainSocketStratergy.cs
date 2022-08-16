using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.IPC
{
    public class UnixDomainSocketOptions
    {
        /// <summary>
        /// Gets or sets the current path prefix for the socket.
        /// </summary>
        /// <remarks>
        /// Evaluates to the current working directory by default.
        /// </remarks>
        public string SocketPrefix { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the suffix for the socket.
        /// </summary>
        /// <remarks>
        /// Empty by default.
        /// </remarks>
        public string SocketSuffix { get; set; } = string.Empty;
    }

    public partial class IPCTransport
    {
        private class UnixDomainSocketStratergy : IStreamStratergy<UnixDomainSocketOptions>
        {
            private static readonly Dictionary<string, Socket> ServerSockets = new();
            private UnixDomainSocketOptions options = new();

            async Task<Stream> IStreamStratergy.Provide(StreamType type, string streamName, CancellationToken token)
            {
                streamName = options.SocketPrefix + streamName + options.SocketSuffix;
                if (type == StreamType.ClientStream)
                {
                    var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                    token.Register(() => { socket.Close(); });
                    await Connect(streamName, socket, token).ConfigureAwait(false);
                    return new NetworkStream(socket, FileAccess.ReadWrite, true);
                }
                else
                {
                    Socket socket;
                    lock (ServerSockets)
                    {
                        if (!ServerSockets.TryGetValue(streamName, out socket))
                        {
                            if (File.Exists(streamName))
                            {
                                // Socket left over from another process.
                                File.Delete(streamName);
                            }
                            socket = ServerSockets[streamName] = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified); 
                            socket.Bind(new UnixDomainSocketEndPoint(streamName));
                        }
                    }
                    
                    socket.Listen(1);
                    Socket? connectedSocket = null;
#if NET6_0_OR_GREATER
                    connectedSocket = await socket.AcceptAsync(token).ConfigureAwait(false);
#else
                    var disposed = false;
                    var acceptTask = socket.AcceptAsync();
                    token.Register(() =>
                    {
                        if (!disposed)
                        {
                            // Interrupts acceptTask; it does not take cancellationtoken.
                            socket.Close();
                        }
                    });
                    try
                    {
                        connectedSocket = await acceptTask.ConfigureAwait(false);
                    }
                    catch (SocketException e)
                    {
                        if (!token.IsCancellationRequested) throw;
                        throw new OperationCanceledException("Socket accept was cancelled.", e);
                    }
                    finally
                    {
                        disposed = true;
                        socket.Dispose();
                    }
#endif
                    return new NetworkStream(connectedSocket, FileAccess.ReadWrite, false);
                }
            }

            private static async Task Connect(string streamName, Socket socket, CancellationToken cancellationToken)
            {
                bool connected = false;
                while (!connected)
                {
                    try
                    {
#if NET6_0_OR_GREATER
                        await socket.ConnectAsync(new UnixDomainSocketEndPoint(streamName), cancellationToken).ConfigureAwait(false);
#else
                        await socket.ConnectAsync(new UnixDomainSocketEndPoint(streamName)).ConfigureAwait(false);
#endif
                        connected = true;
                    }
                    catch (SocketException)
                    {
                        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            public void WithOptions(UnixDomainSocketOptions options)
            {
                this.options = options;
            }
        }
    }
}
