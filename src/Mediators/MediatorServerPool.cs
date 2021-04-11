using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediatR.IPC
{
    public class MediatorServerPool : IDisposable
    {
        private readonly ISender sender;
        private readonly string poolName;
        private readonly List<MediatorServer> servers = new();

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

        public Task Run()
        {
            return Task.WhenAll(servers.Select(s => s.Run()));
        }

        public void Dispose()
        {
            foreach (var server in servers)
            {
                server.Dispose();
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
