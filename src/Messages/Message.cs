using ProtoBuf;
using System;
using System.Linq;

namespace MediatR.IPC.Messages
{
    [ProtoContract]
    internal class Message
    {
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
    }
}
