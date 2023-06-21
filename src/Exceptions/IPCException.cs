using System;

namespace
#if MEDIATR
MediatR.IPC.Exceptions
#else
Mediator.IPC.Exceptions
#endif
{
    public class IPCException : Exception
    {
        public IPCException()
        {
        }

        public IPCException(string? message) : base(message)
        {
        }
    }
}
