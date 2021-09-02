using ProtoBuf;

namespace MediatR.IPC.Tests
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class VoidRequest : IRequest
    {

    }
}