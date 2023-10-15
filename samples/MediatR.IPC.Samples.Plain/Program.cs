#if MEDIATR
using MediatR.IPC.Samples.AssemblyScan;
using MediatR.IPC.Samples.Common;
using MediatR.IPC.Samples.Common.Requests;
#else
using Mediator.IPC.Samples.AssemblyScan;
using Mediator.IPC.Samples.Common;
using Mediator.IPC.Samples.Common.Requests;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace
#if MEDIATR
MediatR.IPC
#else
Mediator.IPC
#endif
.Samples.Plain
{
    internal class Program : ProgramBase
    {
        private Dictionary<Type, Func<object>> typeRegistry = new Dictionary<Type, Func<object>>
        {
            { typeof(IRequestHandler<ApplicationStateQuery, ApplicationStateDto>), () => new ApplicationStateQueryHandler() },
            { typeof(IRequestHandler<IPCMessageCommand, bool>), () => new IPCMessageCommandHandler() },
            { typeof(IEnumerable<IPipelineBehavior<ApplicationStateQuery, ApplicationStateDto>>), () => Array.Empty<IPipelineBehavior<ApplicationStateQuery, ApplicationStateDto>>() },
            { typeof(IEnumerable<IPipelineBehavior<IPCMessageCommand, bool>>), () => Array.Empty<IPipelineBehavior<IPCMessageCommand, bool>>() },
        };

        private Mediator mediator;

#if MEDIATR
        public Program()
        {
            mediator = new Mediator(ServiceFactory);
        }
#else

        public Program()
        {
            mediator = new Mediator(new SP(ServiceFactory));
        }
        private sealed class SP : IServiceProvider
        {
            private Func<Type, object> serviceFactory;
            public SP(Func<Type, object> serviceFactory) => this.serviceFactory = serviceFactory;
            public object GetService(Type serviceType) => serviceFactory(serviceType);
        }
#endif

        public static async Task Main(string[] args)
        {
            var program = new Program();
            await program.EntryPoint(args);
        }

        protected override ISender GetSender() => mediator;

        private object ServiceFactory(Type serviceType)
        {
            var typeName = PrettyTypeName(serviceType);
            Console.WriteLine($"Resolving {typeName}.");
            if (typeRegistry.ContainsKey(serviceType))
            {
                return typeRegistry[serviceType]();
            }

            var error = $"{typeName} is not registered in {nameof(typeRegistry)}";
            Console.WriteLine(error);
            Debugger.Break();
            throw new InvalidOperationException(error);
        }

        private static string PrettyTypeName(Type t)
        {
            // From https://stackoverflow.com/questions/1533115/get-generictype-name-in-good-format-using-reflection-on-c-sharp/25287378
            if (t.IsArray)
            {
                return PrettyTypeName(t.GetElementType()) + "[]";
            }

            if (t.IsGenericType)
            {
                return string.Format(
                    "{0}<{1}>",
                    t.Name.Substring(0, t.Name.LastIndexOf("`", StringComparison.InvariantCulture)),
                    string.Join(", ", t.GetGenericArguments().Select(PrettyTypeName)));
            }

            return t.Name;
        }
    }
}
