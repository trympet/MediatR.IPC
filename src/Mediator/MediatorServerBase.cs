﻿#if MEDIATR
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
                try
                {
                    await using var stream = await CreateAndRegisterStreamAsync(StreamType.ServerStream).ConfigureAwait(false);
                    var message = await Message.Deserialize(stream, LifetimeToken).ConfigureAwait(false);
                    var request = FindRequest(message)
                        ?? throw new InvalidOperationException($"Request not recognized: {message.Name}");

                    var messageContent = DeserializeContent(message, request.RequestType);
                    await ProcessMessage(request, messageContent, stream).ConfigureAwait(false);
                }
                catch (IOException ex)
                {
                    if (LifetimeToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException("The message processing was cancelled by means of an IO error.", ex, LifetimeToken);
                    }
                }
                catch (OperationCanceledException) when (!LifetimeToken.IsCancellationRequested)
                {
                    // Something caused the message to be interrupted.
                }
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
                await Task.WhenAny(completionSource, cancellationSource).ConfigureAwait(false);
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
            return requestToken.IsCancellationRequested
                ? Task.FromCanceled<int>(requestToken)
                : responseStream.ReadAsync(new byte[1], requestToken).AsTask();
        }
    }
}
