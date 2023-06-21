using MediatR;
using ProtoBuf;

namespace
#if MEDIATR
MediatR.IPC
#else
Mediator.IPC
#endif.Samples.AssemblyScan
{
    [IPCRequest]
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public record IPCMessageCommand : IRequest<bool>
    {
        public string Message { get; init; } = string.Empty;
        public int PID { get; init; }
    }
}
