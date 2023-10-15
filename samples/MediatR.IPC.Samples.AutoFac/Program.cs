using Autofac;
#if MEDIATR
using MediatR.IPC.Samples.Common;
using MediatR.IPC.Samples.Common.Requests;
#else
using Mediator.IPC.Samples.Common;
using Mediator.IPC.Samples.Common.Requests;
#endif
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace
#if MEDIATR
MediatR.IPC
#else
Mediator.IPC
#endif
.Samples.AutoFac
{
    public class Program : ProgramBase
    {
        private static readonly ILifetimeScope scope = BuildContainer();

        public static async Task Main(string[] args)
        {
            var program = new Program();
            await program.EntryPoint(args);
        }

        protected override ISender GetSender()
            => scope.Resolve<ISender>();

        private static ILifetimeScope BuildContainer()
        {
            var builder = new ContainerBuilder();

            // Uncomment to enable polymorphic dispatching of requests, but note that
            // this will conflict with generic pipeline behaviors
            // builder.RegisterSource(new ContravariantRegistrationSource());

            // Mediator itself
            builder
                .RegisterType<Mediator>()
                .As<IMediator>()
                .As<ISender>()
                .As<IPublisher>()
                .InstancePerLifetimeScope();

            // request & notification handlers
#if MEDIATR
            builder.Register<ServiceFactory>(context =>
            {
                var c = context.Resolve<IComponentContext>();
                return t => c.Resolve(t);
            });
#else
            throw new NotSupportedException("Mediator does not support Autofac.");
#endif

            // finally register our custom code (individually, or via assembly scanning)
            // - requests & handlers as transient, i.e. InstancePerDependency()
            // - pre/post-processors as scoped/per-request, i.e. InstancePerLifetimeScope()
            // - behaviors as transient, i.e. InstancePerDependency()
            builder.RegisterAssemblyTypes(typeof(AssemblyScan.AssemblyScan).GetTypeInfo().Assembly)
                .AsImplementedInterfaces();

            builder.RegisterType<ApplicationStateQueryHandler>()
                .AsImplementedInterfaces()
                .InstancePerDependency();

            return builder.Build();
        }
    }
}
