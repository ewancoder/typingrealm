using Microsoft.Extensions.DependencyInjection;
using TypingRealm.Messaging.Serialization;

namespace TypingRealm.Messaging.Broker.Client
{
    public static class RegistrationExtensions
    {
        // TODO: Delete 'Client' suffix after it is moved to separate "Client" assembly.
        public static MessageTypeCacheBuilder AddMessagingBrokerClient(
            this MessageTypeCacheBuilder messageTypes)
        {
            var services = messageTypes.Services;

            services.AddSingleton<IBrokerConnectionListener, BrokerConnectionListener>();

            messageTypes.AddMessagingBrokerMessages();
            return messageTypes;
        }
    }
}
