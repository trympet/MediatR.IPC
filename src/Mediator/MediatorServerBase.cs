using MediatR.IPC.Messages;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.IPC
{
    /// <summary>
    /// Represents the base class used by all mediator servers.
    /// </summary>
    public abstract class MediatorServerBase : IPCMediator, IDisposable
    {
        protected MediatorServerBase(string pipeName) : base(pipeName) { }

        /// <summary>
        /// Runs the server.
        /// </summary>
        /// <returns>A task that completes once the server is disposed.</returns>
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
                using var pipe = await PrepareStreamAsync(StreamType.ServerStream, token).ConfigureAwait(false);
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
                await ProcessMessageUntilCancellation(request, messageContent, pipe, token).ConfigureAwait(false);
            }
        }

        private protected abstract Task ProcessMessage(Request request, object message, Stream responseStream, CancellationToken token);

        private protected async Task ProcessMessageUntilCancellation(Request request, object message, Stream responseStream, CancellationToken token)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            var requestToken = cts.Token;
            try
            {
                var completionSource = ProcessMessage(request, message, responseStream, requestToken);
                var cancellationSource = GetStreamEofTask(responseStream, requestToken);
                await Task.WhenAny(completionSource, cancellationSource).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            finally
            {
                // Either an EOF is received or the request is complete.
                // Regardless, we need to cancel the other task.
                cts.Cancel();
            }
        }

        private static Task<int> GetStreamEofTask(Stream responseStream, CancellationToken requestToken)
        {
            return responseStream.ReadAsync(new byte[1], requestToken).AsTask();
        }

        private static Request? FindRequest(Message message)
        {
            return Requests.FirstOrDefault(r => r.Name == message.Name);
        }
    }
}
