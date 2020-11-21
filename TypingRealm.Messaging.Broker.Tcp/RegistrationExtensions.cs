using Microsoft.Extensions.DependencyInjection;

namespace TypingRealm.Messaging.Broker.Tcp
{
    public static class RegistrationExtensions
    {
        public static IServiceCollection AddTcpBrokerConnection(this IServiceCollection services)
        {
            return services.AddSingleton<ITcpBrokerConnectionFactory, TcpBrokerConnectionFactory>();
        }
    }
}
