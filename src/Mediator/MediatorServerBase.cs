using MediatR.IPC.Messages;
using System;
using System.IO;
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

        /// <summary>
        /// Processes and acts upon an incoming message or request.
        /// </summary>
        /// <param name="request">The original request received by the server.</param>
        /// <param name="message">The deserialized content of the message.</param>
        /// <param name="responseStream">A stream for sending a response.</param>
        /// <param name="token">A lifetime token for the current message.</param>
        /// <returns>A task which completes once the message is processed.</returns>
        protected abstract Task ProcessMessage(Request request, object message, Stream responseStream, CancellationToken token);

        private async Task RunUntilCancellation()
        {
            while (!LifetimeToken.IsCancellationRequested)
            {
                await using var stream = await CreateAndRegisterStreamAsync(StreamType.ServerStream).ConfigureAwait(false);
                var message = await Message.Deserialize(stream, LifetimeToken).ConfigureAwait(false);
                var request = FindRequest(message)
                    ?? throw new InvalidOperationException($"Request not recognized: {message.Name}");

                var messageContent = DeserializeContent(message, request.RequestType);
                await ProcessMessage(request, messageContent, stream).ConfigureAwait(false);
            }
        }

        private async Task ProcessMessage(Request request, object message, Stream responseStream)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(LifetimeToken);
            var requestToken = cts.Token;
            try
            {
                var completionSource = ProcessMessage(request, message, responseStream, requestToken);
                var cancellationSource = GetStreamEofTask(responseStream, requestToken);
                var result = await Task.WhenAny(completionSource, cancellationSource).ConfigureAwait(false);
                if (result == cancellationSource)
                {
                    await completionSource.ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (IOException)
            {
                // Broken socket.
            }
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
    }
}
