#if MEDIATR
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
MediatR.IPC.Notifications
#else
Mediator.IPC.Notifications
#endif
{
    public class MediatorNotificationClient : MediatorServerBase
    {
        public MediatorNotificationClient(string pipeName)
            : base(pipeName) { }

        public event EventHandler<INotification>? Notification;

        protected override Task ProcessMessage(Request request, object message, Stream _, CancellationToken token)
        {
            if (message is INotification notification)
            {
                Notification?.Invoke(this, notification);
            }

            return Task.CompletedTask;
        }
    }
}
