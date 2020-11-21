using System.Threading.Tasks;

namespace TypingRealm.Messaging.Broker.Tcp
{
    public interface ITcpBrokerConnectionFactory
    {
        ValueTask ConnectAsync(string host, int port);
        IConnection GetConnection();
    }
}
