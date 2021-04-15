using ProtoBuf;

namespace MediatR.IPC.Samples.Common.Requests
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public record ApplicationStateDto
    {
        public int ProcessId { get; init; }
        public bool IsRunning { get; init; }
    }
}
