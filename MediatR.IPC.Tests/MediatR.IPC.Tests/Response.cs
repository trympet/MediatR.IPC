using ProtoBuf;

namespace MediatR.IPC.Tests
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class Response
    {
        public int A { get; set; }
        public bool B { get; set; }
        public string C { get; set; }
    }
}