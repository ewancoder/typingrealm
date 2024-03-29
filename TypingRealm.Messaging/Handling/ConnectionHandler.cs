﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TypingRealm.Messaging.Connecting;
using TypingRealm.Messaging.Connections;
using TypingRealm.Messaging.Messages;
using TypingRealm.Messaging.Serialization;
using TypingRealm.Messaging.Updating;

namespace TypingRealm.Messaging.Handling;

public sealed class ConnectionHandler : IConnectionHandler
{
    private readonly ILogger<ConnectionHandler> _logger;
    private readonly IConnectionInitializer _connectionInitializer;
    private readonly IConnectedClientStore _connectedClients;
    private readonly IMessageDispatcher _messageDispatcher;
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly IUpdateDetector _updateDetector;
    private readonly IMessageTypeCache _messageTypeCache;
    private readonly IEnumerable<IConnectedClientInitializer> _connectedClientInitializers;
    private readonly IUpdater _updater;

    public ConnectionHandler(
        ILogger<ConnectionHandler> logger,
        IConnectionInitializer connectionInitializer,
        IConnectedClientStore connectedClients,
        IMessageDispatcher messageDispatcher,
        IQueryDispatcher queryDispatcher,
        IUpdateDetector updateDetector,
        IMessageTypeCache messageTypeCache,
        IEnumerable<IConnectedClientInitializer> connectedClientInitializers,
        IUpdater updater)
    {
        _logger = logger;
        _connectionInitializer = connectionInitializer;
        _connectedClients = connectedClients;
        _messageDispatcher = messageDispatcher;
        _queryDispatcher = queryDispatcher;
        _updateDetector = updateDetector;
        _messageTypeCache = messageTypeCache;
        _connectedClientInitializers = connectedClientInitializers;
        _updater = updater;
    }

    public async Task HandleAsync(IConnection connection, CancellationToken cancellationToken)
    {
        ConnectedClient connectedClient;

        connection = connection.WithReceiveAcknowledgement();
        var unwrapperConnection = new ServerMessageUnwrapperConnection(connection);

        try
        {
            connectedClient = await _connectionInitializer.ConnectAsync(unwrapperConnection, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            try
            {
                await connection.SendAsync(new Disconnected($"Error during connection initialization."), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch { }

            throw;
        }

        _connectedClients.Add(connectedClient);
        if (!_connectedClients.IsClientConnected(connectedClient.ClientId))
            throw new InvalidOperationException("Client was not added correctly.");

        foreach (var initializer in _connectedClientInitializers)
        {
            await initializer.InitializeAsync(connectedClient)
                .ConfigureAwait(false);
        }

        // TODO: Unit test all the logic about idempotency.
        var idempotencyKeys = new Dictionary<string, DateTime>();

        // TODO: Send only to groups that were specified in Metadata from the client (if they were sent).
        await TrySendPendingUpdates(connectedClient.Groups, cancellationToken).ConfigureAwait(false);
        while (_connectedClients.IsClientConnected(connectedClient.ClientId))
        {
            MessageMetadata? metadata = null;

            try
            {
                var message = await connection.ReceiveAsync(cancellationToken).ConfigureAwait(false);

                if (message is not MessageWithMetadata messageWithMetadata)
                    throw new InvalidOperationException($"Message is not of {typeof(MessageWithMetadata).Name} type.");

                metadata = messageWithMetadata.Metadata;

                if (messageWithMetadata.Metadata?.MessageId != null && idempotencyKeys.ContainsKey(messageWithMetadata.Metadata.MessageId))
                {
                    _logger.LogDebug(
                        "Message with id {MessageId} has already been handled. Skipping duplicate (idempotency).",
                        messageWithMetadata.Metadata.MessageId);
                    continue;
                }

                await DispatchMessageAsync(connectedClient, messageWithMetadata.Message, cancellationToken).ConfigureAwait(false);

                // TODO: Unit test this.
                if (messageWithMetadata.Metadata?.ResponseMessageTypeId != null)
                {
                    // TODO: Send query response in background, do not block connection handling.
                    var responseType = _messageTypeCache.GetTypeById(messageWithMetadata.Metadata.ResponseMessageTypeId);
                    var response = await _queryDispatcher.DispatchAsync(connectedClient, messageWithMetadata.Message, responseType, cancellationToken)
                        .ConfigureAwait(false);

                    // TODO: Do this also in background.
                    await connectedClient.Connection.SendAsync(response, new MessageMetadata
                    {
                        MessageId = messageWithMetadata.Metadata.MessageId
                    }, cancellationToken)
                        .ConfigureAwait(false);
                }

                // If everything was dispatched successfully:
                if (messageWithMetadata.Metadata?.MessageId != null && !idempotencyKeys.ContainsKey(messageWithMetadata.Metadata.MessageId))
                    idempotencyKeys.Add(messageWithMetadata.Metadata.MessageId, DateTime.UtcNow);

                foreach (var item in idempotencyKeys.Where(x => x.Value < DateTime.UtcNow - TimeSpan.FromMinutes(1)))
                {
                    idempotencyKeys.Remove(item.Key);
                }

                if (messageWithMetadata.Metadata?.AcknowledgementType == AcknowledgementType.Handled && messageWithMetadata.Metadata.MessageId != null)
                {
                    var serverToClientMetadata = new MessageMetadata
                    {
                        MessageId = messageWithMetadata.Metadata.MessageId
                    };

                    await connectedClient.Connection.SendAsync(new AcknowledgeHandled(messageWithMetadata.Metadata.MessageId), serverToClientMetadata, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch
            {
                _connectedClients.Remove(connectedClient.ClientId);
                throw; // If you delete this line and have ncrunch, your PC will die.
            }
            finally
            {
                var groups = connectedClient.Groups;

                // TODO: Unit test this (affected groups).
                if ((metadata as ClientToServerMessageMetadata)?.AffectedGroups != null)
                    groups = groups.Where(group => (metadata as ClientToServerMessageMetadata)?.AffectedGroups?.Contains(group) ?? false);

                // TODO: Do not empty groups if we came here from catch - when client is disconnected we need to notify everyone.
                if (metadata == null || !metadata.SendUpdate)
                    groups = Enumerable.Empty<string>();

                await TrySendPendingUpdates(groups, cancellationToken).ConfigureAwait(false);
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
            _logger.LogError(
                exception,
                "There was an error when handling {MessageName} message for {SenderClientId} client ID.",
                message.GetType().Name,
                sender.ClientId);

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
                _logger.LogError(
                    exception,
                    "Error during sending Disconnected message after message handling failed.");
            }
        }
    }

    private async ValueTask TrySendPendingUpdates(IEnumerable<string> groups, CancellationToken cancellationToken)
    {
        try
        {
            _updateDetector.MarkForUpdate(groups);

            var clientsThatNeedUpdate = _connectedClients.FindInGroups(_updateDetector.PopMarked()).ToList();

            await AsyncHelpers.WhenAll(clientsThatNeedUpdate
                .Select(c => _updater.SendUpdateAsync(c, cancellationToken))).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // TODO: Disconnect player if update was unsuccessful. Currently it silently continues working (investigate).
            _logger.LogError(exception, "Error during sending pending updates.");
        }
    }
}
