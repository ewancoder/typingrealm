using System;
using System.Threading;
using System.Threading.Tasks;

namespace TypingRealm.Messaging.Broker.Client
{
    public interface IBrokerConnectionListener
    {
        Task ListenAsync(CancellationToken cancellationToken);
        ValueTask SubscribeToAsync<TMessage>(
            string type,
            Func<TMessage, ValueTask> handler,
            CancellationToken cancellationToken);
    }
}
