using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TypingRealm.Authentication;
using TypingRealm.Messaging.Client.Handling;
using TypingRealm.Messaging.Connections;
using TypingRealm.Messaging.Messages;
using TypingRealm.Messaging.Serialization;

namespace TypingRealm.Messaging.Client
{
    public interface IAuthenticationService
    {
        ValueTask AuthenticateAsync(
            MessageProcessor processor,
            string characterId,
            CancellationToken cancellationToken);
    }

    public sealed class AuthenticationService : IAuthenticationService
    {
        private readonly IProfileTokenProvider _profileTokenProvider;

        public AuthenticationService(IProfileTokenProvider profileTokenProvider)
        {
            _profileTokenProvider = profileTokenProvider;
        }

        public async ValueTask AuthenticateAsync(
            MessageProcessor processor,
            string characterId,
            CancellationToken cancellationToken)
        {
            var token = await _profileTokenProvider.SignInAsync()
                .ConfigureAwait(false);

            await processor.SendAsync(new Authenticate(token), cancellationToken)
                .ConfigureAwait(false);

            // TODO: Support passing group here.
            await processor.SendAsync(new Connect(characterId), cancellationToken)
                .ConfigureAwait(false);
        }
    }

    // TODO: Introduce heartbeat and heartbeat timeout when we try to reconnect after no reply.
    public sealed class MessageProcessor : AsyncManagedDisposable
    {
        private sealed class QueuedMessage
        {
            public QueuedMessage(
                object message,
                Action<ClientToServerMessageMetadata>? metadataSetter,
                bool requireAcknowledgement,
                CancellationToken cancellationToken)
            {
                Message = message;
                MetadataSetter = metadataSetter;
                RequireAcknowledgement = requireAcknowledgement;
                CancellationToken = cancellationToken;
            }

            public object Message { get; }
            public Action<ClientToServerMessageMetadata>? MetadataSetter { get; }
            public bool RequireAcknowledgement { get; }
            public CancellationToken CancellationToken { get; }
            public TaskCompletionSource Tcs { get; } = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private readonly ILogger<MessageProcessor> _logger;
        private readonly IClientConnectionFactory _connectionFactory;
        private readonly IMessageDispatcher _dispatcher;
        private readonly IProfileTokenProvider _profileTokenProvider;
        private readonly IClientToServerMessageMetadataFactory _metadataFactory;
        private readonly IMessageTypeCache _messageTypeCache;
        private readonly IAuthenticationService _authenticationService;
        private readonly SemaphoreSlimLock _reconnectLock = new SemaphoreSlimLock();
        private readonly ConcurrentDictionary<string, Func<object, ValueTask>> _handlers
            = new ConcurrentDictionary<string, Func<object, ValueTask>>();
        private readonly ConcurrentDictionary<string, Func<object, string?, ValueTask>> _handlersWithId
            = new ConcurrentDictionary<string, Func<object, string?, ValueTask>>();

        private static readonly int _reconnectRetryCount = 3;

        // TODO: Dispose of it.
        private readonly CancellationTokenSource _processingCts = new CancellationTokenSource();
        private readonly SemaphoreSlimLock _processingLock = new SemaphoreSlimLock();
        private Task? _processing;
        private QueuedMessage? _currentMessage;
        private readonly ConcurrentQueue<QueuedMessage> _messageQueue
            = new ConcurrentQueue<QueuedMessage>();

        private ConnectionResource? _resource;

        public MessageProcessor(
            ILogger<MessageProcessor> logger,
            IClientConnectionFactory connectionFactory,
            IMessageDispatcher dispatcher,
            IProfileTokenProvider profileTokenProvider,
            IClientToServerMessageMetadataFactory metadataFactory,
            IMessageTypeCache messageTypeCache,
            IAuthenticationService authenticationService)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
            _dispatcher = dispatcher;
            _profileTokenProvider = profileTokenProvider;
            _metadataFactory = metadataFactory;
            _messageTypeCache = messageTypeCache;
            _authenticationService = authenticationService;
        }

        public bool IsConnected { get; private set; }

        private async Task ProcessMessagesAsync()
        {
            if (_currentMessage == null && _messageQueue.IsEmpty)
                return;

            await using var _ = await _processingLock.UseWaitAsync(_processingCts.Token)
                .ConfigureAwait(false);

            while (IsConnected)
            {
                var current = GetNextQueuedMessage();
                if (current == null)
                    return;

                var reconnectedTimes = 0;
                while (true)
                {
                    try
                    {
                        await SendAsyncInternal(
                            current.Message,
                            current.MetadataSetter,
                            current.RequireAcknowledgement,
                            current.CancellationToken)
                            .ConfigureAwait(false);

                        _currentMessage = null;
                        current.Tcs.SetResult();
                        break;
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, $"Error while sending message: {current.Message.GetType()}");

                        if (reconnectedTimes == _reconnectRetryCount)
                        {
                            _logger.LogError(exception, $"Reached maximum about of retries. Failed to send the message.");
                            current.Tcs.SetException(exception);
                            return;
                        }

                        IsConnected = false;

                        await ReconnectAsync().ConfigureAwait(false);
                        reconnectedTimes++;
                    }
                }
            }
        }

        private QueuedMessage? GetNextQueuedMessage()
        {
            if (_currentMessage != null)
                return _currentMessage;

            if (_messageQueue.TryDequeue(out var message))
            {
                _currentMessage = message;
                return message;
            }

            return null;
        }

        // This method is used within a lock so it shouldn't wait for lock itself anywhere inside.
        public async ValueTask ConnectAsync(string characterId, CancellationToken cancellationToken)
        {
            if (IsConnected)
                throw new InvalidOperationException("Already connected.");

            var connectionWithDisconnect = await _connectionFactory.ConnectAsync(cancellationToken)
                .ConfigureAwait(false);

            var savedMessages = _messageQueue.ToArray();
            _messageQueue.Clear();

            try
            {
                // Need to set it before calling ListenAndDispatchAsync.
                IsConnected = true;

                _resource = new ConnectionResource(
                    connectionWithDisconnect,
                    characterId,
                    cancellationToken);

                Task listening(CancellationToken cancellationToken)
                    => ListenAndDispatchAsync();

                _resource.SetListening(listening);

                await _authenticationService.AuthenticateAsync(this, characterId, _resource.CombinedCts.Token)
                    .ConfigureAwait(false);
            }
            finally
            {
                foreach (var message in savedMessages)
                {
                    _messageQueue.Enqueue(message);
                }
            }
        }

        public ValueTask SendWithoutAcknowledgementAsync(
            object message,
            CancellationToken cancellationToken)
            => SendWithoutAcknowledgementAsync(message, null, cancellationToken);

        public ValueTask SendWithoutAcknowledgementAsync(
            object message,
            Action<ClientToServerMessageMetadata>? metadataSetter,
            CancellationToken cancellationToken)
            => SendAsync(message, metadataSetter, false, cancellationToken);

        public async ValueTask SendAsync(
            object message,
            Action<ClientToServerMessageMetadata>? metadataSetter,
            bool requireAcknowledgement,
            CancellationToken cancellationToken)
        {
            var queuedMessage = new QueuedMessage(
                message,
                metadataSetter,
                requireAcknowledgement,
                cancellationToken);

            _messageQueue.Enqueue(queuedMessage);

            _processing = ProcessMessagesAsync();

            await queuedMessage.Tcs.Task
                .ConfigureAwait(false);
        }

        private async ValueTask SendAsyncInternal(
            object message,
            Action<ClientToServerMessageMetadata>? metadataSetter,
            bool requireAcknowledgement,
            CancellationToken cancellationToken)
        {
            string? subscriptionId = null; // For acknowledgment.

            if (!IsConnected)
                throw new InvalidOperationException("Not connected.");

            var metadata = _metadataFactory.CreateFor(message);
            metadataSetter?.Invoke(metadata);

            var reconnectedTimes = 0;
            while (reconnectedTimes < _reconnectRetryCount)
            {
                // This creates a deadlock. Come up with another solution.
                // Do not allow starting new operations while reconnect is happening.
                // await WaitForReconnectAsync(cancellationToken).ConfigureAwait(false);

                var resource = GetConnectionResource();

                try
                {
                    TaskCompletionSource? tcs = null;

                    if (requireAcknowledgement)
                    {
                        // Setup subscription for acknowledgment.

                        tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                        ValueTask Handler(AcknowledgeReceived acknowledgeReceived)
                        {
                            if (acknowledgeReceived.MessageId == metadata.MessageId)
                                tcs.SetResult();

                            return default;
                        }

                        subscriptionId = SubscribeWithMessageId<AcknowledgeReceived>(Handler, metadata.MessageId);
                        metadata.RequireAcknowledgement = true;
                    }

                    await resource.UseCombinedCts(ct => resource.Connection.SendAsync(message, metadata, ct), cancellationToken)
                        .ConfigureAwait(false);

                    if (requireAcknowledgement)
                    {
                        // Wait for acknowledgment, and if not received - throw.

                        if (tcs == null)
                            throw new InvalidOperationException("Should never happen.");

                        await tcs.Task.WithTimeoutAsync(TimeSpan.FromSeconds(1))
                            .ConfigureAwait(false);
                    }

                    return;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Error while trying to send a message.");
                    reconnectedTimes++;
                    IsConnected = false;

                    if (reconnectedTimes == _reconnectRetryCount)
                        throw;

                    await ReconnectAsync()
                        .ConfigureAwait(false);
                }
                finally
                {
                    if (requireAcknowledgement)
                    {
                        Unsubscribe(subscriptionId!);
                    }
                }
            }
        }

        /*public async ValueTask<TResponse> SendQueryAsync<TResponse>(object message, CancellationToken cancellationToken)
            where TResponse : class
        {
            var tcs = new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            string? subscriptionId = null;

            try
            {
                // We're passing original token here because SendAsync method will have passed combined one.
                await SendWithoutAcknowledgementAsync(message, metadata =>
                {
                    ValueTask Handler(TResponse result)
                    {
                        tcs.SetResult(result);
                        return default;
                    }

                    subscriptionId = SubscribeWithMessageId<TResponse>(Handler, metadata.MessageId);

                    metadata.ResponseMessageTypeId = _messageTypeCache.GetTypeId(typeof(TResponse));
                }, cancellationToken)
                    .ConfigureAwait(false);

                var response = await tcs.Task.WithTimeoutAsync(TimeSpan.FromSeconds(1))
                    .ConfigureAwait(false);

                // TODO: Test how it behaves when timeout occurs.

                return response;
            }
            finally
            {
                Unsubscribe(subscriptionId!);
            }
        }*/

        /// <summary>
        /// Sends message with acknowledgment enabled.
        /// </summary>
        public ValueTask SendAsync(object message, CancellationToken cancellationToken)
            => SendWithAcknowledgementAsync(message, cancellationToken);

        // I Cannot use SendQuery method because it works only after initial connection has been established.
        // But we need to have acknowledgment on the level of all messages (like Authentication, that are used during connection stage).
        public ValueTask SendWithAcknowledgementAsync(
            object message,
            CancellationToken cancellationToken)
            => SendAsync(message, null, true, cancellationToken);

        public string Subscribe<TMessage>(Func<TMessage, ValueTask> handler)
        {
            var subscriptionId = Guid.NewGuid().ToString();

            _handlers.TryAdd(subscriptionId, message =>
            {
                if (message is TMessage tMessage)
                    return handler(tMessage);

                return default;
            });

            return subscriptionId;
        }

        public string SubscribeWithMessageId<TMessage>(Func<TMessage, ValueTask> handler, string? messageId)
        {
            var subscriptionId = Guid.NewGuid().ToString();

            _handlersWithId.TryAdd(subscriptionId, (message, id) =>
            {
                if (message is TMessage tMessage && id == messageId)
                    return handler(tMessage);

                return default;
            });

            return subscriptionId;
        }

        public void Unsubscribe(string subscriptionId)
        {
            _handlers.TryRemove(subscriptionId, out _);
            _handlersWithId.TryRemove(subscriptionId, out _);
        }

        protected override async ValueTask DisposeManagedResourcesAsync()
        {
            var resource = _resource;

            if (resource == null)
                return;

            await resource.DisposeAsync().ConfigureAwait(false);
        }

        // Here comes original cancellation token.
        private async Task ListenAndDispatchAsync()
        {
            var resource = GetConnectionResource();

            while (IsConnected)
            {
                try
                {
                    var message = await resource!.Connection.ReceiveAsync(resource.CombinedCts.Token)
                        .ConfigureAwait(false);

                    if (message is not ServerToClientMessageWithMetadata messageWithMetadata)
                        throw new InvalidOperationException($"Message is not of {typeof(ServerToClientMessageWithMetadata).Name} type.");

                    message = messageWithMetadata.Message;

                    switch (message)
                    {
                        case Disconnected:
                            // Hack to go to reconnect.
                            throw new InvalidOperationException("Server send Disconnected message. Reconnecting...");
                            break;
                        case TokenExpired:
                            var token = await _profileTokenProvider.SignInAsync().ConfigureAwait(false);
                            _ = SendWithoutAcknowledgementAsync(new Authenticate(token), resource.CombinedCts.Token);
                            break;
                    }

                    var tasks = _handlers.Values.Select(handler => handler(message))
                        .Concat(_handlersWithId.Values.Select(handler => handler(message, messageWithMetadata.Metadata.RequestMessageId)))
                        .Append(_dispatcher.DispatchAsync(message, resource.CombinedCts.Token))
                        .ToList();

                    await AsyncHelpers.WhenAll(tasks)
                        .ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Receiving message from the server or dispatching it to one of the handlers failed.");

                    // 1. Wait until all Send operations finish (consider canceling local CTS), do not allow any new Send operations to start.
                    IsConnected = false;

                    _ = ReconnectAsync().ConfigureAwait(false);
                    return;
                }
            }
        }

        private async Task ReconnectAsync()
        {
            if (IsConnected)
                return;

            var resource = GetConnectionResource();

            await using var _ = await _reconnectLock.UseWaitAsync(resource.OriginalCancellationToken)
                .ConfigureAwait(false);

            if (IsConnected)
                return;

            if (_processing != null)
            {
                await _processing.ConfigureAwait(false);
            }

            // Also stops Listening (receiving) for messages.
            await resource.DisposeAsync()
                .ConfigureAwait(false);

            await ConnectAsync(resource.CharacterId, resource.OriginalCancellationToken).ConfigureAwait(false);
        }

        private ConnectionResource GetConnectionResource()
        {
            var resource = _resource;

            if (resource is null)
                throw new InvalidOperationException("Connection resource has been erased. Cannot continue.");

            return resource;
        }
    }
}
