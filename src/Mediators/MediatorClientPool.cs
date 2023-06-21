using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace
#if MEDIATR
MediatR.IPC
#else
Mediator.IPC
#endif
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

        public async
#if MEDIATR
            Task<TResponse>
#else
            ValueTask<TResponse>
#endif
            Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
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

        public async
#if MEDIATR
            Task<object?>
#else
            ValueTask<object?>
#endif
            Send(object request, CancellationToken cancellationToken = default)
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

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            throw IPCMediator.GetAsyncStreamNotSupportedException();
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            throw IPCMediator.GetAsyncStreamNotSupportedException();
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
