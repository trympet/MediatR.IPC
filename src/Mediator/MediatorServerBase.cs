using MediatR.IPC.Messages;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.IPC
{
    public abstract class MediatorServerBase : IPCMediator, IDisposable
    {
        protected MediatorServerBase(string pipeName) : base(pipeName) { }

        public async Task Run()
        {
            try
            {
                await RunUntilCancellation().ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
        }

        private async Task RunUntilCancellation()
        {
            var token = Token;

            while (!token.IsCancellationRequested)
            {
                using var pipe = await PrepareStreamAsync(StreamType.ServerStream, CancellationToken.None).ConfigureAwait(false);
                var buffer = new byte[4096];
                var numBytes = await pipe.ReadAsync(buffer, token);
                var result = new Memory<byte>(buffer).Slice(0, numBytes);
                var message = Serializer.Deserialize<Message>(result);

                var request = FindRequest(message);
                if (request is null)
                {
                    Debug.Fail($"Request not recognized: {message.Name}");
                    continue;
                }

                object messageContent = DeserializeContent(message, request.RequestType);
                await ProcessMessage(request, messageContent, pipe).ConfigureAwait(false);
            }
        }

        private protected abstract Task ProcessMessage(Request request, object message, Stream responseStream);

        private static Request? FindRequest(Message message)
        {
            return Requests.FirstOrDefault(r => r.Name == message.Name);
        }
    }
}
