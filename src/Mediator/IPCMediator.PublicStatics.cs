#if MEDIATR
using MediatR.IPC;
using MediatR.IPC.Messages;
#else
using Mediator.IPC;
using Mediator.IPC.Messages;
#endif
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace
#if MEDIATR
MediatR
#else
Mediator
#endif
{
    public abstract partial class IPCMediator
    {
        private static readonly Type NotificationType = typeof(INotification);
        private static readonly Type RequestType = typeof(IRequest<>);
        [DynamicallyAccessedMembers(DynamicAccess.ContractType | DynamicallyAccessedMemberTypes.Interfaces)]
        private static readonly Type UnitType = typeof(Unit);
        private static TypeModel? CurrentTypeModel;

        internal static readonly Dictionary<string, Request> Requests = new();

        public static TypeModel TypeModel
        {
            get => CurrentTypeModel ?? throw new InvalidOperationException("Type model not set. Ensure IPCMediator.TypeModel is set.");
            set => CurrentTypeModel = value;
        }

        private static readonly List<IPCBuilderContext<IEnumerable<Type>>> UnfinalizedRequests
            = new List<IPCBuilderContext<IEnumerable<Type>>>();

        /// <summary>
        /// Registers types in an assembly.
        /// </summary>
        /// <param name="assembly">The assembly to register types from.</param>
        /// <returns>A builder to constrain the types that are to be registered.</returns>
        [RequiresUnreferencedCode("Dynamic access of assembly types.")]
        public static IPCBuilder<Assembly, IEnumerable<Type>> RegisterAssemblyTypes(Assembly assembly)
        {
            var types = assembly.ExportedTypes;
            var requests = assembly.ExportedTypes
                .Where<Type>(t => t
                    .GetInterfaces()
                    .Any(i => IsRequest(i) || IsNotification(i))
            );

            var builder = new IPCBuilder<Assembly, IEnumerable<Type>>(requests);
            UnfinalizedRequests.Add(builder.BuilderContext);

            return builder;
        }

        private static bool IsRequest(Type i)
            => i.IsGenericType && i.GetGenericTypeDefinition() == RequestType;


        private static bool IsNotification(Type i)
            => i == NotificationType;

        /// <summary>
        /// Registers a request to be handled by the IPC mediator.
        /// </summary>
        /// <typeparam name="T">Return type of the request</typeparam>
        /// <param name="request">The request to register</param>
        /// <returns>A builder for registering more requests.</returns>
        public static void RegisterType<[DynamicallyAccessedMembers(DynamicAccess.ContractType | DynamicallyAccessedMemberTypes.Interfaces)] T>()
            where T : IBaseRequest
        {
            var requestType = typeof(T);
            AddRequest(Finalize(requestType));
        }

        /// <summary>
        /// Registers a request with a given response.
        /// </summary>
        /// <remarks>
        /// This overload does not utilize reflection. An invalid parameter for <paramref name="request"/> or <paramref name="response"/> can lead to runtime failures.
        /// </remarks>
        /// <param name="request">The concrete type of the request; an instance of <see cref="MediatR.IRequest{TResponse}"/>.</param>
        /// <param name="response">The type corresponding to the <c>TResponse</c> type argument in <see cref="MediatR.IRequest{TResponse}"/>.</param>
        public static void RegisterType([DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type request, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type response)
        {
            AddRequest(new Request(request, response));
        }

        /// <summary>
        /// Registers a list of request types.
        /// </summary>
        /// <param name="types"></param>
        [RequiresUnreferencedCode("Unable to annotate params array.")]
        public static void RegisterTypes(params Type[] types)
        {
            foreach (var request in types)
            {
                AddRequest(Finalize(request));
            }
        }

        [Obsolete]
        public static TypeModel GetRuntimeTypeModel()
            => TypeModel;

        public static void UseTransport(IStreamStratergy stratergy)
        {
            if (streamStratergy != IPCTransport.Default)
            {
                throw new InvalidOperationException("Transport can only be configured once.");
            }

            streamStratergy = stratergy;
        }

        public static IStreamStratergy<T> UseTransport<T>(IStreamStratergy<T> stratergy)
        {
            UseTransport((IStreamStratergy)stratergy);

            return stratergy;
        }

        private static void FinalizeUnfinalized()
        {
            var unfinalizedTypes = UnfinalizedRequests.SelectMany(r => r.Value);
            foreach (var unfinalized in unfinalizedTypes)
            {
#pragma warning disable IL2072 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.
                AddRequest(Finalize(unfinalized));
#pragma warning restore IL2072 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.
            }

            UnfinalizedRequests.Clear();
        }

        private static Request Finalize([DynamicallyAccessedMembers(DynamicAccess.ContractType | DynamicallyAccessedMemberTypes.Interfaces)] Type request)
        {
            var response = request.GetInterfaces()
                .FirstOrDefault(i => i == RequestType)
                ?.GenericTypeArguments
                ?.FirstOrDefault();

#pragma warning disable IL2072 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.
            return new Request(request, response ?? UnitType);
#pragma warning restore IL2072 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.
        }

        private static void AddRequest(Request request) => Requests[request.Name] = request;
    }
}
