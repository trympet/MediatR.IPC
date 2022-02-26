using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.IPC.Messages
{
    [ProtoContract]
    internal class Message
    {
        private const int LengthPrefixFieldNumber = 10;

        /// <summary>
        /// Instantiates a new instance of a Message with a binary content.
        /// </summary>
        public Message(string Name, byte[] content)
        {
            this.Name = Name;
            Content = content;
        }

        /// <summary>
        /// Instantiates a new instance of a Message with an exception response.
        /// </summary>
        public Message(Exception e)
        {
            HasError = true;
            ErrorMessage = e.Message;
        }

        /// <summary>
        /// Instantiates a new instance of a Message representing a null response.
        /// </summary>
        public Message()
        {
            Name = "NULL";
        }

        private static RuntimeTypeModel TypeModel => IPCMediator.Serializer;


        [ProtoMember(1)]
        public string Name { get; set; } = string.Empty;

        [ProtoMember(2)]
        public MessageType MessageType { get; set; }

        [ProtoMember(3)]
        public byte[] Content { get; set; } = Array.Empty<byte>();

        [ProtoMember(4)]
        public bool HasError { get; private set; }

        [ProtoMember(5)]
        public string? ErrorMessage { get; private set; }

        public bool IsNullResponse => !Content.Any() && Name == "NULL";

        public static async Task<Message> Deserialize(Stream stream, CancellationToken cancellationToken)
        {
            // TypeModel.DeserializeWithLengthPrefix does not work. We'll do it ourselves.
            const int BufferCapacity = 256; // This is arbitrary.
            var bufferWriter = new System.Buffers.ArrayBufferWriter<byte>();
            var expectedLength = await GetMessageLength(stream, cancellationToken).ConfigureAwait(false);
            while (bufferWriter.WrittenCount < expectedLength)
            {
                var buffer = bufferWriter.GetMemory(BufferCapacity);
                var byteCount = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                bufferWriter.Advance(byteCount);
            }

            Debug.Assert(bufferWriter.WrittenCount == expectedLength, $"Expected {expectedLength}, but got {bufferWriter.WrittenCount}");
            return TypeModel.Deserialize<Message>(bufferWriter.WrittenMemory);
        }

        public void Serialize(Stream stream)
        {
            TypeModel.SerializeWithLengthPrefix(stream, this, typeof(Message), ProtoBuf.PrefixStyle.Base128, LengthPrefixFieldNumber);
        }

        private static async Task<uint> GetMessageLength(Stream stream, CancellationToken cancellationToken)
        {
            const uint msbMask = 0x7F;
            var buffer = new byte[1];
            int offset = 0;
            bool isMsbSet = true;
            uint sum = 0;

            await ProcessFieldPrefix(stream, buffer, cancellationToken).ConfigureAwait(false);

            while (isMsbSet)
            {
                var shift = offset;
                offset += await stream.ReadAsync(buffer, 0, 1, cancellationToken).ConfigureAwait(false);
                var part = buffer[0];
                isMsbSet = part >> 7 == 0x1; // varint is described here: https://developers.google.com/protocol-buffers/docs/encoding
                sum |= (part & msbMask) << (shift * 7);
            }

            return sum;
        }

        private static async Task ProcessFieldPrefix(Stream stream, byte[] buffer, CancellationToken cancellationToken)
        {
            const int varintWireType = 2;
            const int fieldTypeShift = 3;
            var bytesRead = await stream.ReadAsync(buffer, 0, 1, cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                throw new TaskCanceledException("End of stream has been reached.");
            }

            Debug.Assert((buffer[0] & 0x07) == varintWireType && ((buffer[0] >> fieldTypeShift) == LengthPrefixFieldNumber));
        }
    }
}
