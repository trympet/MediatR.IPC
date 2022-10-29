using MediatR.IPC.Exceptions;
using MediatR.IPC.Messages;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.IPC
{
    /// <summary>
    /// Represents a mediator client, used for sending messages.
    /// </summary>
    public class MediatorClient : MediatorClientBase, ISender
    {
        private readonly SemaphoreSlim? pipeSemaphore;

        public MediatorClient(string name, uint id)
            : this(name, id, true) { }

        internal MediatorClient(string name, uint id, bool threadSafe)
            : base($"{name}{(char)(id + 65)}")
        {
            ThreadSafe = threadSafe;
            if (threadSafe)
            {
                pipeSemaphore = new SemaphoreSlim(1, 1);
            }
        }

#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.MemberNotNullWhen(true, nameof(pipeSemaphore))]
#endif
        private bool ThreadSafe { get; }

        /// <inheritdoc/>
        /// <exception cref="IPCException">Thrown when the server sends back an exception.</exception>
        /// <exception cref="TaskCanceledException">Thrown if cancellation is requested.</exception>
        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (ThreadSafe)
            {
                await pipeSemaphore!.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            try
            {
                var response = await SendImpl(request, cancellationToken).ConfigureAwait(false);
                return DeserializeResponse<TResponse>(response);
            }
            finally
            {
                if (ThreadSafe)
                {
                    pipeSemaphore!.Release();
                }
            }
        }

        /// <inheritdoc/>
        public async Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            if (ThreadSafe)
            {
                await pipeSemaphore!.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            try
            {
                var response = await SendImpl(request, cancellationToken).ConfigureAwait(false);
                var type = FindRequest(response) ?? throw new InvalidOperationException("Response not recognized.");
                return DeserializeResponse(response, type.ResponseType);
            }
            finally
            {
                if (ThreadSafe)
                {
                    pipeSemaphore!.Release();
                }
            }
        }

        private async Task<Message> SendImpl<T>(T request, CancellationToken cancellationToken) where T : notnull
        {
            await using var pipe = await CreateAndRegisterStreamAsync(StreamType.ClientStream, cancellationToken).ConfigureAwait(false);
            await SendMessageAsync(request, pipe).ConfigureAwait(false);
            var message = await Message.Deserialize(pipe, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return message;
        }

        private TResponse DeserializeResponse<TResponse>(Message response)
            => (TResponse)DeserializeResponse(response, typeof(TResponse));

        private static object DeserializeResponse(Message response, Type contentType)
        {
            if (response.HasError)
            {
                throw new IPCException(response.ErrorMessage);
            }

            if (response.IsNullResponse)
            {
                return default(Unit);
            }

            return DeserializeContent(response, contentType);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            throw GetAsyncStreamNotSupportedException();
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            throw GetAsyncStreamNotSupportedException();
        }
    }
}
