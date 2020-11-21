using System.Threading;
using System.Threading.Tasks;

namespace TypingRealm.Messaging.Broker
{
    public sealed class SubscribeToHandler : IMessageHandler<SubscribeTo>
    {
        private readonly IClientSubscriptionStore _clientSubscriptionStore;

        public SubscribeToHandler(IClientSubscriptionStore clientSubscriptionStore)
        {
            _clientSubscriptionStore = clientSubscriptionStore;
        }

        public ValueTask HandleAsync(ConnectedClient sender, SubscribeTo message, CancellationToken cancellationToken)
        {
            _clientSubscriptionStore.AddSubscription(message.Type, sender);
            return default;
        }
    }
}
