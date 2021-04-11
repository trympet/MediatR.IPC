using MediatR;
using MediatR.IPC;
using System.Threading;
using System.Threading.Tasks;

namespace MediatR.IPC.Notifications
{
    public class MediatorPublisher : MediatorClientBase, IPublisher
    {
        public MediatorPublisher(string pipeName)
            : base(pipeName) { }

        public async Task Publish<TNotification>(TNotification notification)
            where TNotification : INotification, new()
        {
            using var pipe = await PrepareStreamAsync(StreamType.ClientStream, CancellationToken.None).ConfigureAwait(false);
            await SendMessageAsync(notification, pipe);
        }

        public async Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            using var pipe = await PrepareStreamAsync(StreamType.ClientStream, cancellationToken).ConfigureAwait(false);
            await SendMessageAsync(notification, pipe);
        }

        public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            using var pipe = await PrepareStreamAsync(StreamType.ClientStream, cancellationToken).ConfigureAwait(false);
            await SendMessageAsync(notification, pipe);
        }
    }
}
