using ProtoBuf;

namespace MediatR.IPC.Samples.Common.Requests
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class ApplicationStateQuery : IRequest<ApplicationStateDto>
    {
        public int Id { get; set; }
    }
}
