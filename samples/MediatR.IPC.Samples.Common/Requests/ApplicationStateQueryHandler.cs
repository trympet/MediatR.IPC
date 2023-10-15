using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if MEDIATR
using ResponseType = System.Threading.Tasks.Task<MediatR.IPC.Samples.Common.Requests.ApplicationStateDto>;
using TaskType = System.Threading.Tasks.Task;
#else
using ResponseType = System.Threading.Tasks.ValueTask<Mediator.IPC.Samples.Common.Requests.ApplicationStateDto>;
using TaskType = System.Threading.Tasks.ValueTask;
#endif

namespace
#if MEDIATR
MediatR.IPC
#else
Mediator.IPC
#endif
.Samples.Common.Requests
{
    public class ApplicationStateQueryHandler : IRequestHandler<ApplicationStateQuery, ApplicationStateDto>
    {
        public ResponseType Handle(ApplicationStateQuery request, CancellationToken cancellationToken)
        {
            var result = new ApplicationStateDto
            {
                IsRunning = true,
                ProcessId = Process.GetCurrentProcess().Id,
            };

            return TaskType.FromResult(result);
        }
    }
}
