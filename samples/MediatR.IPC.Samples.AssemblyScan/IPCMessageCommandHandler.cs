using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace
#if MEDIATR
MediatR.IPC
#else
Mediator.IPC
#endif
.Samples.AssemblyScan
{
    public class IPCMessageCommandHandler : IRequestHandler<IPCMessageCommand, bool>
    {
        private static readonly Random Random = new Random();

        public
#if MEDIATR
            Task<bool>
#else
            ValueTask<bool>
#endif
            Handle(IPCMessageCommand request, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Got message from PID {request.PID}: {request.Message}");

            var response = Random.Next(0, 1) == 0;
            Console.WriteLine($"Sending {response} as response.");

#if MEDIATR
            return Task.FromResult(response);
#else
            return ValueTask.FromResult(response);
#endif
        }
    }
}
