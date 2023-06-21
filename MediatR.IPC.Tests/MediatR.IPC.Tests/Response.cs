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
    public class Response
    {
        public int A { get; set; }
        public bool B { get; set; }
        public string C { get; set; }
    }
}