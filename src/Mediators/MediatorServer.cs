using MediatR.IPC.Messages;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.IPC
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

        private protected override async Task ProcessMessage(Request request, object message, Stream responseStream, CancellationToken token)
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

            await SendResponseAsync(request, response, responseStream);
            //object? response;
            //try
            //{
            //    using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            //    var requestTask = sender.Send(message, cts.Token);
            //    var streamEofTask = responseStream.ReadAsync(new byte[1], cts.Token);
            //    var res = await Task.WhenAny(requestTask, streamEofTask.AsTask()).ConfigureAwait(false);
            //    if (res is Task<object?>  res1)
            //    {
            //        response = res1.Result;
            //    }
            //    else
            //    {
            //        cts.Cancel();
            //        throw new TaskCanceledException();
            //    }
            //}
            //catch (Exception e)
            //{
            //    response = e;
            //}

            //await SendResponseAsync(request, response, responseStream);
        }

        private async Task SendResponseAsync(Request request, object? response, Stream stream)
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
                var responseSerialized = await SerializeRequestAsync(response).ConfigureAwait(false);
                responseMessage = new Message(request.Name, responseSerialized);
            }
            Serializer.Serialize(stream, responseMessage);
        }
    }
}
