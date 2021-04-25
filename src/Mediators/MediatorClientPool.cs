using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.IPC
{
    public class MediatorClientPool : ISender
    {
        private readonly SemaphoreSlim poolSemaphore;
        private readonly Stack<MediatorClient> clients;
        private readonly string poolName;

        public MediatorClientPool(string poolName, int poolSize)
        {
            if (poolSize > 10 || poolSize < 1)
            {
                throw new ArgumentException("Pool must be < 10 and > 1.", nameof(poolSize));
            }

            poolSemaphore = new SemaphoreSlim(poolSize, poolSize);
            clients = new Stack<MediatorClient>(poolSize);
            this.poolName = poolName;
            PopulatePool(clients, poolSize);
        }

        private void PopulatePool(Stack<MediatorClient> pool, int poolSize)
        {
            for (uint i = 0; i < poolSize; i++)
            {
                pool.Push(new MediatorClient(poolName, i));
            }
        }

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            await poolSemaphore.WaitAsync(cancellationToken);
            var client = clients.Pop();
            try
            {
                return await client.Send(request, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                clients.Push(client);
                poolSemaphore.Release();
            }
        }

        public async Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            await poolSemaphore.WaitAsync(cancellationToken);
            var client = clients.Pop();
            try
            {
                return await client.Send(request, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                clients.Push(client);
                poolSemaphore.Release();
            }
        }
    }
}
