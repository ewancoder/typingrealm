﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TypingRealm.Messaging.Connecting;
using TypingRealm.Messaging.Messages;
using TypingRealm.Messaging.Updating;

namespace TypingRealm.Messaging.Handling
{
    public sealed class ConnectionHandler : IConnectionHandler
    {
        private readonly ILogger<ConnectionHandler> _logger;
        private readonly IConnectionInitializer _connectionInitializer;
        private readonly IConnectedClientStore _connectedClients;
        private readonly IMessageDispatcher _messageDispatcher;
        private readonly IUpdateDetector _updateDetector;
        private readonly IUpdater _updater;

        public ConnectionHandler(
            ILogger<ConnectionHandler> logger,
            IConnectionInitializer connectionInitializer,
            IConnectedClientStore connectedClients,
            IMessageDispatcher messageDispatcher,
            IUpdateDetector updateDetector,
            IUpdater updater)
        {
            _logger = logger;
            _connectionInitializer = connectionInitializer;
            _connectedClients = connectedClients;
            _messageDispatcher = messageDispatcher;
            _updateDetector = updateDetector;
            _updater = updater;
        }

        public async Task HandleAsync(IConnection connection, CancellationToken cancellationToken)
        {
            ConnectedClient connectedClient;
            try
            {
                connectedClient = await _connectionInitializer.ConnectAsync(connection, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                try
                {
                    await connection.SendAsync(new Disconnected($"Error during connection initialization."), cancellationToken)
                        .ConfigureAwait(false);
                } catch { }

                throw;
            }

            _connectedClients.Add(connectedClient);
            if (!_connectedClients.IsClientConnected(connectedClient.ClientId))
                throw new InvalidOperationException("Client was not added correctly.");

            await TrySendPendingUpdates(connectedClient.Group, cancellationToken).ConfigureAwait(false);
            while (_connectedClients.IsClientConnected(connectedClient.ClientId))
            {
                try
                {
                    var message = await connection.ReceiveAsync(cancellationToken).ConfigureAwait(false);

                    await DispatchMessageAsync(connectedClient, message, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    _connectedClients.Remove(connectedClient.ClientId);
                    throw; // If you delete this line and have ncrunch, your PC will die.
                }
                finally
                {
                    await TrySendPendingUpdates(connectedClient.Group, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async ValueTask DispatchMessageAsync(ConnectedClient sender, object message, CancellationToken cancellationToken)
        {
            ValueTask? disconnecting = null;

            try
            {
                // The message propagates to all the handlers and waits for them to finish.
                await _messageDispatcher.DispatchAsync(sender, message, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"There was an error when handling {message.GetType().Name} message for {sender.ClientId} client ID.");

#pragma warning disable CA2012 // We store ValueTask in a variable to await it later once.
                disconnecting = sender.Connection.SendAsync(new Disconnected($"Error when handling {message.GetType().Name} message."), cancellationToken);
#pragma warning restore CA2012

                _connectedClients.Remove(sender.ClientId);

                throw;
            }
            finally
            {
                // Do not lose the exception from catch block.
                try
                {
                    if (disconnecting != null)
                    {
                        await disconnecting.Value.ConfigureAwait(false);
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, $"Error during sending Disconnected message after message handling failed.");
                }
            }
        }

        private async ValueTask TrySendPendingUpdates(string group, CancellationToken cancellationToken)
        {
            try
            {
                _updateDetector.MarkForUpdate(group);

                var clientsThatNeedUpdate = _connectedClients.FindInGroups(_updateDetector.PopMarked()).ToList();

                await AsyncHelpers.WhenAll(clientsThatNeedUpdate
                    .Select(c => _updater.SendUpdateAsync(c, cancellationToken))).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                // TODO: Disconnect player if update was unsuccessful. Currently it silently continues working (investigate).
                _logger.LogError(exception, $"Error during sending pending updates.");
            }
        }
    }
}
