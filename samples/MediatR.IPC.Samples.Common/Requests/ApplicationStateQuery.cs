using ProtoBuf;

namespace
#if MEDIATR
MediatR.IPC
#else
Mediator.IPC
#endif
.Samples.Common.Requests
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class ApplicationStateQuery : IRequest<ApplicationStateDto>
    {
        public int Id { get; set; }
    }
}
