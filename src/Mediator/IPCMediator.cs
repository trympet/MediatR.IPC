using MediatR.IPC;
using MediatR.IPC.Messages;
using ProtoBuf.Meta;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR
{
    public abstract partial class IPCMediator : IDisposable
    {
        private static IStreamStratergy streamStratergy = IPCTransport.Default;

        private readonly CancellationTokenSource cts = new();
        private readonly string pipeName;

        private bool disposedValue;

        protected IPCMediator(string pipeName)
        {
            this.pipeName = pipeName;
        }

        protected CancellationToken Token => cts.Token;

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    cts.Cancel();
                    cts.Dispose();
                }

                disposedValue = true;
            }
        }

        protected static Task<byte[]> SerializeRequestAsync<TRequest>(TRequest request)
        {
            return SerializeRequestAsync((object?)request);
        }

        protected static async Task<byte[]> SerializeRequestAsync(object? request)
        {
            using var serializationStream = new MemoryStream();
            Serializer.Serialize(serializationStream, request);
            serializationStream.Position = 0;
            var buffer = new byte[serializationStream.Length];
            await serializationStream.ReadAsync(buffer);
            return buffer;
        }

        protected async Task<Stream> PrepareStreamAsync(StreamType type, CancellationToken token)
        {
            using var compositeCts = CancellationTokenSource.CreateLinkedTokenSource(Token, token);
            var pipeToken = compositeCts.Token;
            pipeToken.ThrowIfCancellationRequested();

            return await streamStratergy.Provide(type, pipeName, pipeToken).ConfigureAwait(false);
        }

        private protected static object DeserializeContent(Message message, Type contentType)
        {
            var x = new ReadOnlySpan<byte>(message.Content);
            var messageContent = Serializer.Deserialize(contentType, x);
            return messageContent;
        }

        private protected static TContent DeserializeContent<TContent>(Message message)
            => (TContent)DeserializeContent(message, typeof(TContent));
    }
}
