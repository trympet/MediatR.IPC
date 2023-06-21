using System;

namespace
#if MEDIATR
MediatR.IPC.Messages
#else
Mediator.IPC.Messages
#endif
{
    public sealed class Request
    {
        private static readonly Type UnitType = typeof(Unit);

        /// <summary>
        /// Instantiates a request with a response.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        public Request(Type request, Type response)
        {
            RequestType = request;
            ResponseType = response;
        }

        /// <summary>
        /// Instantiates a request with a void response.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        public Request(Type request)
        {
            RequestType = request;
            ResponseType = UnitType;
        }

        public string Name => RequestType.FullName ?? "GENERIC-ILLEGAL";

        public Type RequestType { get; internal set; }

        public Type ResponseType { get; internal set; }
    }
}
