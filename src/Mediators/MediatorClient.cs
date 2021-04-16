using MediatR.IPC.Exceptions;
using MediatR.IPC.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.IPC
{
    /// <summary>
    /// Represents a mediator client, used for sending messages.
    /// </summary>
    public class MediatorClient : MediatorClientBase, ISender
    {
        private readonly SemaphoreSlim pipeSemaphore = new(1, 1);

        public MediatorClient(string name, uint id)
            : base($"{name}{(char)(id + 65)}") { }

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            await pipeSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                using var pipe = await PrepareStreamAsync(StreamType.ClientStream, CancellationToken.None).ConfigureAwait(false);
                await SendMessageAsync(request, pipe).ConfigureAwait(false);
                var response = Serializer.Deserialize<Message>(pipe);
                return DeserializeResponse<TResponse>(response);
            }
            finally
            {
                pipeSemaphore.Release();
            }
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => Send(request, cancellationToken);

        private TResponse DeserializeResponse<TResponse>(Message response)
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
    }
}
