using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using TypingRealm.Messaging.Connections;
using TypingRealm.Messaging.Serialization.Protobuf;

namespace TypingRealm.Messaging.Broker.Tcp
{
    // TODO: Implement reconnection & error handling (since it's singleton).
    public sealed class TcpBrokerConnectionFactory : AsyncManagedDisposable, ITcpBrokerConnectionFactory
    {
        private readonly TcpClient _tcpClient;
        private readonly SemaphoreSlimLock _sendLock;
        private readonly SemaphoreSlimLock _receiveLock;
        private readonly IProtobufConnectionFactory _protobufConnectionFactory;
        private Stream? _stream;
        private IConnection? _connection;

        public TcpBrokerConnectionFactory(IProtobufConnectionFactory protobufConnectionFactory)
        {
            _tcpClient = new TcpClient();
            _sendLock = new SemaphoreSlimLock();
            _receiveLock = new SemaphoreSlimLock();
            _protobufConnectionFactory = protobufConnectionFactory;
        }

        public async ValueTask ConnectAsync(string host, int port)
        {
            await _tcpClient.ConnectAsync(host, port).ConfigureAwait(false);
            _stream = _tcpClient.GetStream();
            _connection = _protobufConnectionFactory.CreateProtobufConnection(_stream)
                .WithLocking(_sendLock, _receiveLock);
        }

        public IConnection GetConnection()
        {
            if (_connection == null)
                throw new InvalidOperationException("Not connected yet. First call Connect method.");

            return _connection;
        }

        protected override async ValueTask DisposeManagedResourcesAsync()
        {
            if (_stream != null)
                await _stream.DisposeAsync().ConfigureAwait(false);

            await _sendLock.DisposeAsync().ConfigureAwait(false);
            await _receiveLock.DisposeAsync().ConfigureAwait(false);

            _tcpClient.Dispose();
        }
    }
}
