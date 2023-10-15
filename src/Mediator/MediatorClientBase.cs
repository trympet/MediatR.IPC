#if MEDIATR
using MediatR.IPC.Messages;
#else
using Mediator.IPC.Messages;
#endif
using System;
using System.IO;
using System.Threading.Tasks;

namespace
#if MEDIATR
MediatR.IPC
#else
Mediator.IPC
#endif
{
    /// <summary>
    /// Represents the base class used by mediator clients.
    /// </summary>
    public abstract class MediatorClientBase : IPCMediator
    {
        protected MediatorClientBase(string pipeName)
            : base(pipeName) { }

        /// <summary>
        /// Sends a message to the server.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <param name="message">The message.</param>
        /// <param name="stream">The stream to send the message over.</param>
        /// <returns></returns>
        protected static Task SendMessageAsync<TMessage>(TMessage message, Stream stream)
            where TMessage : notnull
        {
            var requestSerialized = SerializeContent<TMessage>(message);
            return SendRequest(stream, requestSerialized, message.GetType());
        }

        /// <summary>
        /// Sends a message to the server.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <param name="message">The message.</param>
        /// <param name="stream">The stream to send the message over.</param>
        /// <returns></returns>
        protected static Task SendMessageAsync(object message, Stream stream)
        {
            var requestSerialized = SerializeContent(message);
            return SendRequest(stream, requestSerialized, message.GetType());
        }

        private static async Task SendRequest(Stream stream, ReadOnlyMemory<byte> requestSerialized, Type messageType)
        {
            var request = new Message(messageType.FullName ?? "UNKNOWN", requestSerialized);
            request.Serialize(stream);
            await stream.FlushAsync().ConfigureAwait(false);
        }
    }
}
