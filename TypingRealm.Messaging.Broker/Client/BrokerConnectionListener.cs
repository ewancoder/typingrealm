using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TypingRealm.Messaging.Broker.Tcp;

namespace TypingRealm.Messaging.Broker.Client
{
    // Should be singleton, or move out singleton handler cache.
    public sealed class BrokerConnectionListener : IBrokerConnectionListener
    {
        // TODO: Move the connection address to configuration / Communication.
        // TODO: Use abstract connection factory, not TCP. !! and remove reference to TCP assembly.
        // This is a temparory hack.
        private readonly ITcpBrokerConnectionFactory _connectionFactory;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<Func<object, ValueTask>>> _handlers
            = new ConcurrentDictionary<string, ConcurrentQueue<Func<object, ValueTask>>>();
        private readonly ConcurrentDictionary<string, Type> _types
            = new ConcurrentDictionary<string, Type>();

        public BrokerConnectionListener(ITcpBrokerConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private IConnection? _brokerConnection;

        public async Task ListenAsync(CancellationToken cancellationToken)
        {
            await _connectionFactory.ConnectAsync("127.0.0.1", 30200)
                .ConfigureAwait(false);

            _brokerConnection = _connectionFactory.GetConnection();

            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await _brokerConnection.ReceiveAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (message is PublicMessage publicMessage)
                {
                    if (!_handlers.TryGetValue(publicMessage.Type, out var handlers))
                        continue;

                    if (!_types.TryGetValue(publicMessage.Type, out var type))
                        throw new InvalidOperationException($"Cannot handle message, type is missing for: {publicMessage.Type}.");

                    foreach (var handler in handlers)
                    {
                        // TODO: Use interface for serialization and refactor common settings to Serialization.Core.
                        var deserialized = JsonSerializer.Deserialize(publicMessage.Data, type);
                        if (deserialized == null)
                            throw new InvalidOperationException($"Serializer returns null for message: {publicMessage.Type}.");

                        await handler(deserialized).ConfigureAwait(false);
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        public ValueTask SubscribeToAsync<TMessage>(
            string type,
            Func<TMessage, ValueTask> handler,
            CancellationToken cancellationToken)
        {
            if (_brokerConnection == null)
                throw new InvalidOperationException("Cannot subscribe when not connected.");

            if (!_handlers.TryGetValue(type, out var handlers))
                _handlers.TryAdd(type, new ConcurrentQueue<Func<object, ValueTask>>());

            if (!_handlers.TryGetValue(type, out handlers))
                throw new InvalidOperationException("Impossible: Concurrent dictionary failed to add the value.");

            // TODO: Somehow move the possible cast exception up the stack to synchronously notify about incompatible types.
            handlers.Enqueue(async message => await handler((TMessage)message).ConfigureAwait(false));

            _types.AddOrUpdate(type, typeof(TMessage), (stringType, actualType) =>
            {
                if (actualType != typeof(TMessage))
                    throw new InvalidOperationException($"Cannot subscribe to the same type usind different generic type: {typeof(TMessage).Name}.");

                return actualType;
            });

            return _brokerConnection.SendAsync(new SubscribeTo(type), cancellationToken);
        }
    }
}
