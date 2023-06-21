using ProtoBuf;

namespace
#if MEDIATR
MediatR.IPC
#else
Mediator.IPC
#endif.Samples.Common.Requests
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public record ApplicationStateDto
    {
        public int ProcessId { get; init; }
        public bool IsRunning { get; init; }
    }
}
