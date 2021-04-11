using MediatR.IPC;
using MediatR.IPC.Messages;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MediatR
{
    public abstract partial class IPCMediator
    {
        private static readonly Type RequestType = typeof(IRequest<>);
        private static readonly Type UnitType = typeof(Unit);

        private protected static readonly HashSet<Request> Requests = new HashSet<Request>();

        private protected static readonly RuntimeTypeModel Serializer = InitializeRuntimeTypeModel();

        private static readonly List<IPCBuilderContext<IEnumerable<Type>>> UnfinalizedRequests
            = new List<IPCBuilderContext<IEnumerable<Type>>>();

        /// <summary>
        /// Registers types in an assembly.
        /// </summary>
        /// <param name="assembly">The assembly to register types from.</param>
        /// <returns>A builder to constrain the types that are to be registered.</returns>
        public static IPCBuilder<Assembly, IEnumerable<Type>> RegisterAssemblyTypes(Assembly assembly)
        {
            var types = assembly.ExportedTypes;
            var requests = assembly.ExportedTypes
                .Where(t => t
                    .GetInterfaces()
                    .Any(i => i == RequestType)
                );

            var builder = new IPCBuilder<Assembly, IEnumerable<Type>>(requests);
            UnfinalizedRequests.Add(builder.BuilderContext);

            return builder;
        }

        /// <summary>
        /// Registers a request to be handled by the IPC mediator.
        /// </summary>
        /// <typeparam name="T">Return type of the request</typeparam>
        /// <param name="request">The request to register</param>
        /// <returns>A builder for registering more requests.</returns>
        public static void RegisterType<T>()
            where T : IBaseRequest
        {
            var requestType = typeof(T);
            Requests.Add(Finalize(requestType));
        }

        /// <summary>
        /// Registers a list of request types.
        /// </summary>
        /// <param name="types"></param>
        public static void RegisterTypes(params Type[] types)
        {
            foreach (var request in types)
            {
                Requests.Add(Finalize(request));
            }
        }

        public static RuntimeTypeModel GetRuntimeTypeModel()
            => Serializer;

        public static void UseTransport(IStreamStratergy stratergy)
        {
            if (streamStratergy != IPCTransport.Default)
            {
                throw new InvalidOperationException("Transport can only be configured once.");
            }

            streamStratergy = stratergy;
        }

        private static Request Finalize(Type request)
        {
            var response = request.GetInterfaces()
                .FirstOrDefault(i => i == RequestType)
                ?.GenericTypeArguments
                ?.FirstOrDefault();

            return new Request(request, response ?? UnitType);
        }

        private static RuntimeTypeModel InitializeRuntimeTypeModel()
        {
            var typeModel = RuntimeTypeModel.Create("MediatorTypeModel");
            typeModel.AutoAddMissingTypes = true;
            return typeModel;
        }
    }
}
