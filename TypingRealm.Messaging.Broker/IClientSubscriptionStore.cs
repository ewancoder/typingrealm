using System.Collections.Generic;

namespace TypingRealm.Messaging.Broker
{
    public interface IClientSubscriptionStore
    {
        IEnumerable<ConnectedClient> GetSubscribedClientsFor(string type);
        void AddSubscription(string type, ConnectedClient client);
    }
}
