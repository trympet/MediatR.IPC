#if MEDIATR
using MediatR.IPC.Messages;
#else
using Mediator.IPC.Messages;
#endif
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace
#if MEDIATR
MediatR.IPC
#else
Mediator.IPC
#endif
{
    /// <summary>
    /// Represents a runner for receiving and dispatching IPC messages.
    /// </summary>
    public class MediatorServer : MediatorServerBase
    {
        private readonly ISender sender;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediatorServer"/> class.
        /// </summary>
        /// <param name="sender">The sender to use for dispatching events.</param>
        /// <param name="name">The name of the pool.</param>
        /// <param name="id">The id of the server.</param>
        public MediatorServer(ISender sender, string name, uint id = 0)
            : base($"{name}{(char)(id + 65)}")
        {
            this.sender = sender;
        }

        protected override async Task ProcessMessage(Request request, object message, Stream responseStream, CancellationToken token)
        {
            object? response;
            try
            {
                response = await sender.Send(message, token).ConfigureAwait(false);
            }
            catch (TaskCanceledException) { throw; }
            catch (Exception e)
            {
                response = e;
            }

            token.ThrowIfCancellationRequested();
            await SendResponseAsync(request, response, responseStream, token).ConfigureAwait(false);
        }

        private static async Task SendResponseAsync(Request request, object? response, Stream stream, CancellationToken token)
        {
            Message responseMessage;
            if (response is Exception e)
            {
                responseMessage = new Message(e);
            }
            else if (response is Unit)
            {
                responseMessage = new Message();
            }
            else
            {
                var responseSerialized = await SerializeContentAsync(response).ConfigureAwait(false);
                responseMessage = new Message(request.Name, responseSerialized);
            }
            token.ThrowIfCancellationRequested();
            responseMessage.Serialize(stream);
            await stream.FlushAsync(token).ConfigureAwait(false);
        }
    }
}
