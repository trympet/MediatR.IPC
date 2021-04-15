using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.IPC.Samples.Common.Requests
{
    public class ApplicationStateQueryHandler : IRequestHandler<ApplicationStateQuery, ApplicationStateDto>
    {
        public Task<ApplicationStateDto> Handle(ApplicationStateQuery request, CancellationToken cancellationToken)
        {
            var result = new ApplicationStateDto
            {
                IsRunning = true,
                ProcessId = Process.GetCurrentProcess().Id,
            };

            return Task.FromResult(result);
        }
    }
}
