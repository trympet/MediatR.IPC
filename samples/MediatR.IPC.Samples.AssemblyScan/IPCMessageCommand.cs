using MediatR;
using ProtoBuf;

namespace MediatR.IPC.Samples.AssemblyScan
{
    [IPCRequest]
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public record IPCMessageCommand : IRequest<bool>
    {
        public string Message { get; init; } = string.Empty;
        public int PID { get; init; }
    }
}
