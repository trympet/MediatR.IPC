using MediatR.IPC.Messages;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MediatR.IPC.Notifications
{
    public class MediatorNotificationClient : MediatorServerBase
    {
        public MediatorNotificationClient(string pipeName)
            : base(pipeName) { }

        public event EventHandler<INotification>? Notification;

        private protected override Task ProcessMessage(Request request, object message, Stream _)
        {
            if (message is INotification notification)
            {
                Notification?.Invoke(this, notification);
            }

            return Task.CompletedTask;
        }
    }
}
