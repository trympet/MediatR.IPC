using System.Threading;
using System.Threading.Tasks;

namespace
#if MEDIATR
MediatR.IPC.Notifications
#else
Mediator.IPC.Notifications
#endif
{
    public class MediatorPublisher : MediatorClientBase, IPublisher
    {
        public MediatorPublisher(string pipeName)
            : base(pipeName) { }

        public async
#if MEDIATR
            Task
#else
            ValueTask
#endif
            Publish(object notification, CancellationToken cancellationToken = default)
        {
            using var pipe = await CreateAndRegisterStreamAsync(StreamType.ClientStream, cancellationToken).ConfigureAwait(false);
            await SendMessageAsync(notification, pipe).ConfigureAwait(false);
        }

        public async
#if MEDIATR
            Task
#else
            ValueTask
#endif
            Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            using var pipe = await CreateAndRegisterStreamAsync(StreamType.ClientStream, cancellationToken).ConfigureAwait(false);
            await SendMessageAsync<TNotification>(notification, pipe).ConfigureAwait(false);
        }
    }
}
