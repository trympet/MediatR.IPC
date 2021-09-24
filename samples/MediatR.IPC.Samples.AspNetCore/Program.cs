using MediatR.IPC.Samples.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR.IPC.Samples.Common.Requests;

namespace MediatR.IPC.Samples.AspNetCore
{
    public class Program : ProgramBase
    {
        private readonly IServiceProvider serviceScope;

        public Program(IServiceProvider serviceScope)
        {
            this.serviceScope = serviceScope;
        }

        // Run without IIS Express.
        public static async Task Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "-c")
            {
                var builder = CreateHostBuilder(args);
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<ISender>(new MediatorClientPool(poolName: "demo", poolSize: 4));
                });
                builder.Build().Run();
            }
            else
            {
                var builder = CreateHostBuilder(args);
                builder.ConfigureServices(services =>
                {
                    services.AddMediatR(typeof(AssemblyScan.AssemblyScan));
                    services.AddScoped<ApplicationStateQueryHandler>();
                    services.AddScoped<Demo>();
                });
                var host = builder.Build();
                var program = new Program(host.Services);
                await program.EntryPoint(args);
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        protected override ISender GetSender()
        {
            return serviceScope.GetRequiredService<ISender>();
        }
    }
}
