using System;
using System.Collections.Generic;
using System.Text;

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
