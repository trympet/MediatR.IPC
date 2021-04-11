using System;

namespace MediatR.IPC.Exceptions
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
