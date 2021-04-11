using MediatR;
using MediatR.IPC;
using MediatR.IPC.Exceptions;
using MediatR.IPC.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.IPCs
{
    public class MediatorClient : MediatorClientBase
    {
        private readonly SemaphoreSlim pipeSemaphore = new(1, 1);

        public MediatorClient(string name, uint id)
            : base($"{name}{(char)(id + 65)}") { }

        public async Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request)
            where TRequest : IRequest<TResponse>
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

        public Task SendAsync<TRequest>(TRequest request)
            where TRequest : IRequest
        {
            return SendAsync<TRequest, Unit>(request);
        }

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
