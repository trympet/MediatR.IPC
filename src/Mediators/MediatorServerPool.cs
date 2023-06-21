using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace
#if MEDIATR
MediatR.IPC
#else
Mediator.IPC
#endif
{
    /// <summary>
    /// Represents a pool of <see cref="MediatorServer"/>. 
    /// </summary>
    public class MediatorServerPool : IDisposable
    {
        private readonly ISender sender;
        private readonly string poolName;
        private readonly List<MediatorServer> servers = new();
        private bool disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediatorServerPool"/> class.
        /// </summary>
        /// <param name="sender">The sender to use for dispatching incoming messages.</param>
        /// <param name="poolName">The name of the IPC pool. Must be the same for client and server.</param>
        /// <param name="poolSize">The number of servers in the IPC pool. Max 10.</param>
        public MediatorServerPool(ISender sender, string poolName, int poolSize)
        {
            if (poolSize > 10 || poolSize < 1)
            {
                throw new ArgumentException("Pool must be < 10 and > 1.", nameof(poolSize));
            }

            this.sender = sender;
            this.poolName = poolName;
            PopulatePool(poolSize);
        }

        /// <summary>
        /// Runs the server pool until it is disposed.
        /// </summary>
        /// <returns></returns>
        public Task Run()
        {
            if (disposedValue)
            {
                throw new ObjectDisposedException(nameof(MediatorServerPool));
            }

            return Task.WhenAll(servers.Select(s => s.Run()));
        }

        /// <summary>
        /// Disposes the server pool and stops receiving incoming events.
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
                    foreach (var server in servers)
                    {
                        server.Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        private void PopulatePool(int poolSize)
        {
            for (uint i = 0; i < poolSize; i++)
            {
                servers.Add(new MediatorServer(sender, poolName, i));
            }
        }
    }
}
