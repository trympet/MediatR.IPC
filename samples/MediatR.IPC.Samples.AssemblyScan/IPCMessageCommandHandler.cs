using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.IPC.Samples.AssemblyScan
{
    internal class IPCMessageCommandHandler : IRequestHandler<IPCMessageCommand, bool>
    {
        private static readonly Random Random = new Random();

        public Task<bool> Handle(IPCMessageCommand request, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Got message from PID {request.PID}: {request.Message}");

            var response = Random.Next(0, 1) == 0;
            Console.WriteLine($"Sending {response} as response.");

            return Task.FromResult(response);
        }
    }
}
