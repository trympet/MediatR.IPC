using System;

namespace MediatR
{
    /// <summary>
    /// Indicates that a request can be sent via an IPC transport.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class IPCRequestAttribute : Attribute { }
}
