using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.IPC
{
    public class MediatorClientPool : ISender, IDisposable
    {
        private readonly SemaphoreSlim poolSemaphore;
        private readonly List<MediatorClient> allClients;
        private readonly ConcurrentStack<MediatorClient> clients;
        private readonly string poolName;

        public MediatorClientPool(string poolName, int poolSize)
        {
            if (poolSize > 10 || poolSize < 1)
            {
                throw new ArgumentException("Pool must be < 10 and > 1.", nameof(poolSize));
            }

            poolSemaphore = new SemaphoreSlim(poolSize, poolSize);
            clients = new ConcurrentStack<MediatorClient>();
            this.poolName = poolName;
            PopulatePool(clients, poolSize);
            allClients = new List<MediatorClient>(clients);
        }

        private void PopulatePool(ConcurrentStack<MediatorClient> pool, int poolSize)
        {
            for (uint i = 0; i < poolSize; i++)
            {
                pool.Push(new MediatorClient(poolName, i));
            }
        }

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            await poolSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            MediatorClient client = await PopClientAsync().ConfigureAwait(false);
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
            await poolSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            var client = await PopClientAsync().ConfigureAwait(false);
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

        public void Dispose()
        {
            foreach (var client in allClients)
            {
                client.Dispose();
            }
        }

        private async Task<MediatorClient> PopClientAsync()
        {
            MediatorClient client;
            while (!clients.TryPop(out client))
            {
                await Task.Delay(1).ConfigureAwait(false);
            }

            return client;
        }
    }
}
