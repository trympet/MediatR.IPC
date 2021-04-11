using MediatR.IPC.Messages;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MediatR.IPC
{
    public class MediatorServer : MediatorServerBase
    {
        private readonly ISender sender;

        public MediatorServer(ISender sender, string name, uint id)
            : base($"{name}{(char)(id + 65)}")
        {
            this.sender = sender;
        }

        private protected override async Task ProcessMessage(Request request, object message, Stream responseStream)
        {
            object? response;
            try
            {
                response = await sender.Send(message).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                response = e;
            }

            await SendResponseAsync(request, response, responseStream);
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
