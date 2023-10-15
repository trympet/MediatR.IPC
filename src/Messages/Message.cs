using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace
#if MEDIATR
MediatR.IPC.Messages
#else
Mediator.IPC.Messages
#endif
{
    [ProtoContract]
    internal class Message
    {
        private const int LengthPrefixFieldNumber = 10;

        /// <summary>
        /// Instantiates a new instance of a Message with a binary content.
        /// </summary>
        public Message(string Name, ReadOnlyMemory<byte> content)
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

        private static TypeModel TypeModel => IPCMediator.TypeModel;

        [ProtoMember(1)]
        public string Name { get; set; } = string.Empty;

        [ProtoMember(2)]
        public MessageType MessageType { get; set; }

        [ProtoMember(3)]
        public ReadOnlyMemory<byte> Content { get; set; } = Array.Empty<byte>();

        [ProtoMember(4)]
        public bool HasError { get; private set; }

        [ProtoMember(5)]
        public string? ErrorMessage { get; private set; }

        public bool IsNullResponse => Content.Length == 0 && Name == "NULL";

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

            return MessageSerializer.Deserialize(bufferWriter.WrittenMemory);
        }

        public void Serialize(Stream stream)
        {
            MessageSerializer.SerializeWithLengthPrefix(stream, this);
        }

        private static async Task<uint> GetMessageLength(Stream stream, CancellationToken cancellationToken)
        {
            const uint msbMask = 0x7F;
            Memory<byte> buffer = new byte[1];
            int offset = 0;
            bool isMsbSet = true;
            uint sum = 0;

            await ProcessFieldPrefix(stream, buffer, cancellationToken).ConfigureAwait(false);

            byte part;
            while (isMsbSet)
            {
                var shift = offset;
                offset += await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                part = buffer.Span[0];
                isMsbSet = part >> 7 == 0x1; // varint is described here: https://developers.google.com/protocol-buffers/docs/encoding
                sum |= (part & msbMask) << (shift * 7);
            }

            return sum;
        }

        private static async Task ProcessFieldPrefix(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            const int varintWireType = 2;
            const int fieldTypeShift = 3;
            var bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                throw new TaskCanceledException("End of stream has been reached.");
            }

            Debug.Assert((buffer.Span[0] & 0x07) == varintWireType && ((buffer.Span[0] >> fieldTypeShift) == LengthPrefixFieldNumber));
        }

        private sealed class MessageSerializer : ISerializer<Message>
        {
            private static readonly MessageSerializer Instance = new MessageSerializer();

            private MessageSerializer()
            {
            }

            public SerializerFeatures Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;

            public static Message Deserialize(in ReadOnlyMemory<byte> source)
            {
                using var state = ProtoReader.State.Create(source, TypeModel);
                return state.DeserializeRoot(new Message(), Instance);
            }

            public static void SerializeWithLengthPrefix(Stream dest, Message value)
            {
                ProtoWriter.State state = ProtoWriter.State.Create(dest, TypeModel);
                try
                {
                    state.WriteMessage(LengthPrefixFieldNumber, Instance.Features, value, Instance);
                    state.Flush();
                    state.Close();
                }
                catch
                {
                    state.Abandon();
                    throw;
                }
                finally
                {
                    state.Dispose();
                }
            }

            public Message Read(ref ProtoReader.State state, Message value)
            {
                value ??= new();
                int num;
                while ((num = state.ReadFieldHeader()) > 0)
                {
                    switch (num)
                    {
                        case 1:
                            {
                                string text = state.ReadString(default(StringMap));
                                if (text != null)
                                {
                                    value.Name = text;
                                }
                                break;
                            }
                        case 2:
                            {
                                MessageType messageType = (MessageType)state.ReadByte();
                                value.MessageType = messageType;
                                break;
                            }
                        case 3:
                            {
                                ReadOnlyMemory<byte> content = value.Content;
                                content = state.AppendBytes(content);
                                value.Content = content;
                                break;
                            }
                        case 4:
                            {
                                bool hasError = state.ReadBoolean();
                                value.HasError = hasError;
                                break;
                            }
                        case 5:
                            {
                                string text = state.ReadString(default(StringMap));
                                if (text != null)
                                {
                                    value.ErrorMessage = text;
                                }
                                break;
                            }
                        default:
                            state.SkipField();
                            break;
                    }
                }
                return value;
            }

            public void Write(ref ProtoWriter.State state, Message value)
            {
                string name = value.Name;
                string text;
                if (name != null)
                {
                    text = name;
                    if (text != string.Empty)
                    {
                        state.WriteString(1, text, default(StringMap));
                    }
                }
                MessageType messageType = value.MessageType;
                if ((int)messageType != 0)
                {
                    state.WriteFieldHeader(2, WireType.Varint);
                    byte b = (byte)(int)messageType;
                    state.WriteByte(b);
                }
                ReadOnlyMemory<byte> content = value.Content;
                state.WriteFieldHeader(3, WireType.String);
                ReadOnlyMemory<byte> array = content;
                state.WriteBytes(array);
                bool hasError = value.HasError;
                if (hasError)
                {
                    state.WriteFieldHeader(4, WireType.Varint);
                    state.WriteBoolean(hasError);
                }
                text = value.ErrorMessage!;
                state.WriteString(5, text, default(StringMap));
            }
        }
    }
}
