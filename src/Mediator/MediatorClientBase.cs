using MediatR.IPC.Messages;
using System.IO;
using System.Threading.Tasks;

namespace MediatR.IPC
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
        protected static async Task SendMessageAsync<TMessage>(TMessage message, Stream stream)
            where TMessage : notnull
        {
            var requestSerialized = await SerializeContentAsync(message).ConfigureAwait(false);
            var request = new Message(message.GetType().FullName ?? "UNKNOWN", requestSerialized);
            request.Serialize(stream);
            await stream.FlushAsync().ConfigureAwait(false);
        }
    }
}
