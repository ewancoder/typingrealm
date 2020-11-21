using Microsoft.Extensions.DependencyInjection;
using TypingRealm.Messaging.Serialization;

namespace TypingRealm.Messaging.Broker
{
    public static class RegistrationExtensions
    {
        public static MessageTypeCacheBuilder AddMessagingBroker(this MessageTypeCacheBuilder messageTypes)
        {
            var services = messageTypes.Services;

            services.AddSingleton<IClientSubscriptionStore, ClientSubscriptionStore>();

            services.RegisterHandler<PublicMessage, PublicMessageHandler>();
            services.RegisterHandler<SubscribeTo, SubscribeToHandler>();

            messageTypes.AddMessagingBrokerMessages();
            return messageTypes;
        }
    }
}
