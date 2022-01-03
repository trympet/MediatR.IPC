using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediatR.IPC.Tests
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class HugeRequest : IRequest<byte[]>
    {
        public byte[] Data { get; set; }
        public static HugeRequest Create(int sizeBytes)
        {
            var data = new byte[sizeBytes];
            Random.Shared.NextBytes(data);
            return new HugeRequest
            {
                Data = data
            };
        }
    }
}
