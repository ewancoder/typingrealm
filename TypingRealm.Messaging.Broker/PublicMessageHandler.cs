using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TypingRealm.Messaging.Broker
{
    public sealed class PublicMessageHandler : IMessageHandler<PublicMessage>
    {
        private readonly IClientSubscriptionStore _clientSubscriptionStore;

        public PublicMessageHandler(IClientSubscriptionStore clientSubscriptionStore)
        {
            _clientSubscriptionStore = clientSubscriptionStore;
        }

        public ValueTask HandleAsync(ConnectedClient sender, PublicMessage message, CancellationToken cancellationToken)
        {
            var clients = _clientSubscriptionStore.GetSubscribedClientsFor(message.Type);

            return AsyncHelpers.WhenAll(clients.Select(client => client.Connection.SendAsync(message, cancellationToken)));
        }
    }
}
