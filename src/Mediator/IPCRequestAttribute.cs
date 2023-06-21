using System;

namespace
#if MEDIATR
MediatR
#else
Mediator
#endif
{
    /// <summary>
    /// Indicates that a request can be sent via an IPC transport.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class IPCRequestAttribute : Attribute { }
}
