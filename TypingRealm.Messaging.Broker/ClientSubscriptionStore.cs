using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TypingRealm.Messaging.Broker
{
    public sealed class ClientSubscriptionStore : IClientSubscriptionStore
    {
        private readonly ConcurrentDictionary<string, ConcurrentQueue<ConnectedClient>> _subscribedClients
            = new ConcurrentDictionary<string, ConcurrentQueue<ConnectedClient>>();

        public IEnumerable<ConnectedClient> GetSubscribedClientsFor(string type)
        {
            if (!_subscribedClients.TryGetValue(type, out var subscribedClients))
                return Enumerable.Empty<ConnectedClient>();

            return subscribedClients;
        }

        public void AddSubscription(string type, ConnectedClient client)
        {
            if (!_subscribedClients.TryGetValue(type, out var clients))
            {
                _subscribedClients.TryAdd(type, new ConcurrentQueue<ConnectedClient>());

                if (!_subscribedClients.TryGetValue(type, out clients))
                    throw new InvalidOperationException("Impossible exception: ConcurrentDictionary did not add element for some reason.");
            }

            clients.Enqueue(client);
        }
    }
}
