using TypingRealm.Messaging.Serialization;

namespace TypingRealm.Messaging.Broker
{
    public static class RegistrationExtensions
    {
        public static MessageTypeCacheBuilder AddMessagingBrokerMessages(this MessageTypeCacheBuilder messageTypes)
        {
            return messageTypes.AddMessageTypesFromAssembly(typeof(PublicMessage).Assembly);
        }
    }
}
