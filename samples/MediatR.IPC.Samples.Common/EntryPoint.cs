using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MediatR.IPC.Samples.Common
{
    public static class EntryPoint
    {
        public static void Main(string[] args)
        {
            IPCMediator.UseTransport(IPCTransport.NamedPipe);

            IPCMediator
                .RegisterAssemblyTypes(Assembly.GetAssembly(typeof(AssemblyScan.AssemblyScan)))
                .WithAttribute<IPCRequestAttribute>();

            IPCMediator
                .RegisterType<ApplicationStateQuery>();
        }
    }
}
