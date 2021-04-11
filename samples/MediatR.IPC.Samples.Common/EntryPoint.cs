using System.Reflection;

namespace MediatR.IPC.Samples.Common
{
    public static class EntryPoint
    {
        public static void Main(string[] args)
        {
            IPCMediator.UseTransport(IPCTransport.NamedPipe);

            IPCMediator
                .RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .WithAttribute<IPCRequestAttribute>();

            IPCMediator
                .RegisterType<ApplicationStateQuery>();

            var server = new MediatorServerPool(sender, "")
        }
    }
}
