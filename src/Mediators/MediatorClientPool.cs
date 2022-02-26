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
        private readonly Stack<MediatorClient> availableClients;
        private readonly string poolName;

        public MediatorClientPool(string poolName, int poolSize)
        {
            if (poolSize > 10 || poolSize < 1)
            {
                throw new ArgumentException("Pool must be < 10 and > 1.", nameof(poolSize));
            }

            poolSemaphore = new SemaphoreSlim(poolSize, poolSize);
            availableClients = new Stack<MediatorClient>();
            this.poolName = poolName;
            PopulatePool(availableClients, poolSize);
            allClients = new List<MediatorClient>(availableClients);
        }

        private void PopulatePool(Stack<MediatorClient> pool, int poolSize)
        {
            for (uint i = 0; i < poolSize; i++)
            {
                pool.Push(new MediatorClient(poolName, i, false));
            }
        }

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            await poolSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            MediatorClient client;
            lock (availableClients)
            {
                client = availableClients.Pop();
            }

            try
            {
                return await client.Send(request, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                lock (availableClients)
                {
                    availableClients.Push(client);
                }
                poolSemaphore.Release();
            }
        }

        public async Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            await poolSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            MediatorClient client;
            lock (availableClients)
            {
                client = availableClients.Pop();
            }

            try
            {
                return await client.Send(request, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                lock (availableClients)
                {
                    availableClients.Push(client);
                }
                poolSemaphore.Release();
            }
        }

        public void Dispose()
        {
            foreach (var client in allClients)
            {
                client.Dispose();
            }
            poolSemaphore.Dispose();
        }
    }
}
