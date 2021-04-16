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

        private protected async Task SendMessageAsync<TMessage>(TMessage message, Stream stream)
            where TMessage : notnull
        {
            var requestSerialized = await SerializeRequestAsync(message).ConfigureAwait(false);
            var request = new Message(message.GetType().FullName, requestSerialized);
            Serializer.Serialize(stream, request);
        }
    }
}
