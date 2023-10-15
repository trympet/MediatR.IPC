using System;
using System.Diagnostics.CodeAnalysis;

namespace
#if MEDIATR
MediatR.IPC.Messages
#else
Mediator.IPC.Messages
#endif
{
    public sealed class Request
    {
        [DynamicallyAccessedMembers(DynamicAccess.ContractType)]
        private static readonly Type UnitType = typeof(Unit);

        /// <summary>
        /// Instantiates a request with a response.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        public Request([DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type request, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type response)
        {
            RequestType = request;
            ResponseType = response;
        }

        /// <summary>
        /// Instantiates a request with a void response.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        public Request([DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type request)
        {
            RequestType = request;
            ResponseType = UnitType;
        }

        public string Name => RequestType.FullName ?? "GENERIC-ILLEGAL";

        [DynamicallyAccessedMembers(DynamicAccess.ContractType)]
        public Type RequestType { get; internal set; }

        [DynamicallyAccessedMembers(DynamicAccess.ContractType)]
        public Type ResponseType { get; internal set; }
    }
}
