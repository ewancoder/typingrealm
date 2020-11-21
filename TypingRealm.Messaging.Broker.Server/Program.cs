using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TypingRealm.Hosting;
using TypingRealm.Messaging.Broker.Client;
using TypingRealm.Messaging.Broker.Tcp;

namespace TypingRealm.Messaging.Broker.Server
{
    public static class Program
    {
        private const int Port = 30200;

        public static async Task Main()
        {
            using var host = HostFactory.CreateTcpHostBuilder(Port, builder =>
            {
                builder.AddMessagingBroker();
            }).Build();

            Task.Run(async () =>
            {
                var broker = host.Services.GetRequiredService<ITcpBrokerConnectionFactory>();
                // TODO: Remove reference to TypingRealm.Messaging.Core.
                var connection = broker.GetConnection();
                await connection.SendAsync()
                // this way it won't work. I need to supply STRING TYPE manually since TypeCache won't be able to resolve type for public messages.
                // !!!!! or build public messages cache globally in some config/static service and get it during startup of services,
                // so all services have the access to all global public messages list.
                // then my framework can be better reused I think (separate instance of IMessageTypeCache).
            });

            await host.RunAsync().ConfigureAwait(false);
        }
    }
}
