﻿using System.IO;
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
    /// A holder class for built-in IPC transports.
    /// </summary>
    public partial class IPCTransport
    {
        /// <summary>
        /// A stream provider which uses named pipes for IPC communication.
        /// </summary>
        public static readonly IStreamStratergy NamedPipe = new NamedPipeStratergy();

#if !NETSTANDARD
        [System.Runtime.Versioning.UnsupportedOSPlatform("windows")]
#endif
        /// <summary>
        /// A stream provider which relies on Unix Domain Sockets. Only supported on Unix (obliviously).
        /// </summary>
        public static readonly IStreamStratergy<UnixDomainSocketOptions> UnixDomainSocket = new UnixDomainSocketStratergy();

        /// <summary>
        /// The default stream provider. Supported on all platforms.
        /// </summary>
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
