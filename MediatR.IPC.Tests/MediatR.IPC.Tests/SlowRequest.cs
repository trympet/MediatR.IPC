using ProtoBuf;

namespace
#if MEDIATR
MediatR.IPC
#else
Mediator.IPC
#endif
.Tests
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class SlowRequest : IRequest<Response>
    {

    }
}