#if MEDIATR
using MediatR.IPC.Exceptions;
using MediatR.IPC.Messages;
#else
using Mediator.IPC.Exceptions;
using Mediator.IPC.Messages;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        public async
#if MEDIATR
            Task<TResponse>
#else
            ValueTask<TResponse>
#endif
            Send<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
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
        public async
#if MEDIATR
            Task<object?>
#else
            ValueTask<object?>
#endif
            Send(object request, CancellationToken cancellationToken = default)
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

        private async Task<Message> SendImpl(object request, CancellationToken cancellationToken)
        {
            await using var pipe = await CreateAndRegisterStreamAsync(StreamType.ClientStream, cancellationToken).ConfigureAwait(false);
            await SendMessageAsync(request, pipe).ConfigureAwait(false);
            var message = await Message.Deserialize(pipe, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return message;
        }

        private static TResponse DeserializeResponse<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] TResponse>(Message response)
        {
            if (response.HasError)
            {
                throw new IPCException(response.ErrorMessage);
            }

            if (response.IsNullResponse)
            {
                return default!;
            }

            return DeserializeContent<TResponse>(response);
        }

        private static object DeserializeResponse(Message response, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type contentType)
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

        public ValueTask<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamQuery<TResponse> query, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamCommand<TResponse> command, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
