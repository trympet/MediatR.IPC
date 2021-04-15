using MediatR.IPC.Samples.AssemblyScan;
using MediatR.IPC.Samples.Common.Requests;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MediatR.IPC.Samples.Common
{
    public class Demo
    {
        private readonly ISender sender;

        public Demo(ISender sender)
        {
            ConfigureIPCMediator();
            this.sender = sender;
        }

        public async Task RunClient(string identifier)
        {
            Console.WriteLine("Starting client..");
            Console.WriteLine($"Pool name: {identifier}");

            ISender clientPool = new MediatorClientPool(poolName: identifier, poolSize: 4);
            Console.WriteLine( $"Sending {nameof(ApplicationStateQuery)} from client to server..");
            var serverProcessInfo = await clientPool.Send(new ApplicationStateQuery());
            Console.WriteLine($"Response received:");
            Console.WriteLine(serverProcessInfo);

            var pid = Process.GetCurrentProcess().Id;
            while(true)
            {
                Console.Write("Send message: ");
                var message = Console.ReadLine();
                Console.WriteLine("Sending message..");
                var response = await clientPool.Send(new IPCMessageCommand { PID = pid, Message = message });
                Console.WriteLine($"Got response: {response}");
            }
        }

        public async Task RunServer(string identifier)
        {
            Console.WriteLine("Starting server..");
            var server = new MediatorServerPool(sender, poolName: identifier, poolSize: 4);

            await server.Run();
        }

        private void ConfigureIPCMediator()
        {
            IPCMediator.UseTransport(IPCTransport.NamedPipe);

            IPCMediator
                .RegisterAssemblyTypes(typeof(AssemblyScan.AssemblyScan).Assembly)
                .WithAttribute<IPCRequestAttribute>();

            IPCMediator
                .RegisterType<ApplicationStateQuery>();
        }
    }
}
