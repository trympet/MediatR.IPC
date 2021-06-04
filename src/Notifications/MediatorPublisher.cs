using System.Threading;
using System.Threading.Tasks;

namespace MediatR.IPC.Notifications
{
    public class MediatorPublisher : MediatorClientBase, IPublisher
    {
        public MediatorPublisher(string pipeName)
            : base(pipeName) { }

        public async Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            using var pipe = await CreateAndRegisterStreamAsync(StreamType.ClientStream, cancellationToken).ConfigureAwait(false);
            await SendMessageAsync(notification, pipe).ConfigureAwait(false);
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => Publish(notification, cancellationToken);
    }
}
