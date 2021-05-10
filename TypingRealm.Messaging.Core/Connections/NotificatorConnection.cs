using System;
using System.Threading;
using System.Threading.Tasks;

namespace TypingRealm.Messaging.Connections
{
    /// <summary>
    /// Connection that uses <see cref="Notificator"/> to be notified about
    /// received messages. It uses <see cref="IMessageSender"/> passed in it's
    /// constructor to send messages.
    /// </summary>
    public sealed class NotificatorConnection : IConnection
    {
        private readonly IMessageSender _messageSender;
        private readonly Notificator _notificator;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificatorConnection"/> class.
        /// </summary>
        /// <param name="messageSender">Used for sending messages.</param>
        /// <param name="notificator">Used for receiving messages.</param>
        public NotificatorConnection(IMessageSender messageSender, Notificator notificator)
        {
            _messageSender = messageSender;
            _notificator = notificator;
        }

        public ValueTask SendAsync(object message, CancellationToken cancellationToken)
            => _messageSender.SendAsync(message, cancellationToken);

        public async ValueTask<object> ReceiveAsync(CancellationToken cancellationToken)
        {
            if (_notificator.ReceivedMessagesBuffer.TryDequeue(out var message))
                return message;

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            void Handle() => tcs.SetResult(true);
            _notificator.Received += Handle;

            try
            {
                if (!_notificator.ReceivedMessagesBuffer.TryDequeue(out message))
                {
                    await tcs.Task
                        .WithCancellationAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (!_notificator.ReceivedMessagesBuffer.TryDequeue(out message))
                        throw new InvalidOperationException("Corrupted state, this should never happen.");
                }

                return message;
            }
            finally
            {
                _notificator.Received -= Handle;
            }
        }
    }
}
