#if MEDIATR
using MediatR.IPC;
using MediatR.IPC.Messages;
#else
using Mediator.IPC;
using Mediator.IPC.Messages;
#endif
using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace
#if MEDIATR
MediatR
#else
Mediator
#endif
{
    public abstract partial class IPCMediator : IDisposable
    {
        private static readonly object finalizerLock = new();
        private static bool finalized;

        private static IStreamStratergy streamStratergy = IPCTransport.Default;

        private readonly CancellationTokenSource cts = new();
        private readonly string pipeName;

        private bool disposedValue;
        private CancellationTokenSource? linkedCts;

        protected IPCMediator(string pipeName)
        {
            this.pipeName = pipeName;
            lock (finalizerLock)
            {
                if (!finalized)
                {
                    FinalizeUnfinalized();
                    finalized = true;
                }
            }

            LifetimeToken = cts.Token;
        }

        protected CancellationToken LifetimeToken { get; }

        /// <summary>
        /// Releases unmanaged resources consumed by this instance.
        /// All ongoing requests are cancelled and control is resumed to the callsite.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    cts.Cancel();
                    cts.Dispose();
                }

                disposedValue = true;
            }
        }

        protected static ReadOnlyMemory<byte> SerializeContent<TRequest>(TRequest request)
        {
            AssertConcrete<TRequest>();
            var bufferWriter = new ArrayBufferWriter<byte>();
            // This uses a different, faster overload than the non-generic one.
            TypeModel.Serialize(bufferWriter, request);
            return bufferWriter.WrittenMemory;
        }

        protected static ReadOnlyMemory<byte> SerializeContent(object? request)
        {
            var bufferWriter = new ArrayBufferWriter<byte>();
            TypeModel.Serialize(bufferWriter, request);
            return bufferWriter.WrittenMemory;
        }

        /// <summary>
        /// Creates a stream and registers it to the lifetime of the current instance.
        /// </summary>
        /// <param name="type">A value indicating what type of stream to create</param>
        /// <param name="token">A token which will signal cancellation to the adversary and close the stream upon cancellation.</param>
        /// <returns>A connected, unidirectional stream.</returns>
        protected async Task<Stream> CreateAndRegisterStreamAsync(StreamType type, CancellationToken token = default)
        {
            linkedCts?.Dispose();
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(LifetimeToken, token);
            var pipeToken = linkedCts.Token;
            pipeToken.ThrowIfCancellationRequested();

            var stream = await streamStratergy.Provide(type, pipeName, pipeToken).ConfigureAwait(false);
            pipeToken.Register(() => stream.Close());
            return stream;

        }

        private protected static Request? FindRequest(Message message)
        {
            return Requests.TryGetValue(message.Name, out var value) ? value : null;
        }

        private protected static object DeserializeContent(Message message, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type contentType)
        {
            var messageContent = TypeModel.Deserialize(contentType, message.Content.Span);
            return messageContent;
        }

        private protected static TContent DeserializeContent<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] TContent>(Message message)
        {
            AssertConcrete<TContent>();
            // This uses a different, faster overload than the non-generic one.
            var messageContent = TypeModel.Deserialize<TContent>(message.Content.Span);
            return messageContent;
        }

        protected internal static Exception GetAsyncStreamNotSupportedException()
        {
            throw new NotSupportedException("Stream methods are not implemented by MediatR.IPC.");
        }

        [Conditional("DEBUG")]
        private static void AssertConcrete<TRequest>()
        {
            Debug.Assert(!typeof(TRequest).IsAbstract && !typeof(TRequest).IsInterface && typeof(TRequest) != typeof(object),
                            "Expected a concrete type than can be serialized.");
        }
    }
}
