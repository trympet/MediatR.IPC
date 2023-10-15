#if MEDIATR
using MediatR.IPC.Samples.Common.Requests;
#else
using Mediator.IPC.Samples.Common.Requests;
#endif
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace
#if MEDIATR
MediatR.IPC
#else
Mediator.IPC
#endif
.Samples.Common
{
    public abstract class ProgramBase
    {
        protected async Task EntryPoint(string[] args)
        {
            if (!args.Any() || args.Length == 2)
            {
                await RunDemo(args);
            }
            else
            {
                PrintUsage();
                return;
            }
        }

        private async Task RunDemo(string[] args)
        {
            var demo = new Demo(GetSender());
            if (!args.Any())
            {
                Fork("-c demo");
                await demo.RunServer("demo");
            }
            else if (args[0] == "-s")
            {
                await demo.RunServer(args[1]);
            }
            else if (args[0] == "-c")
            {
                await demo.RunClient(args[1]);
            }
        }

        protected abstract ISender GetSender();

        private static void Fork(string arguments)
        {
            var exeLocation = Process.GetCurrentProcess().MainModule.FileName;
            var startInfo = new ProcessStartInfo
            {
                FileName = exeLocation,
                Arguments = arguments,
                UseShellExecute = true,
            };

            Process.Start(startInfo);
        }

        private static void PrintUsage()
        {
            var exeName = Assembly.GetEntryAssembly().GetName().Name;
            Console.WriteLine($"usage:\n{exeName} [-s | -c] [NAME]");
        }
    }
}
