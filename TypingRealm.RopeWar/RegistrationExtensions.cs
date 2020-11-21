using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TypingRealm.Hosting;
using TypingRealm.Messaging;
using TypingRealm.Messaging.Broker.Client;
using TypingRealm.Messaging.Connecting;
using TypingRealm.Messaging.Serialization;
using TypingRealm.Profiles.Api.Client;
using TypingRealm.RopeWar.Adapters;
using TypingRealm.RopeWar.Handlers;

namespace TypingRealm.RopeWar
{
    public static class RegistrationExtensions
    {
        public static MessageTypeCacheBuilder AddRopeWar(this MessageTypeCacheBuilder messageTypes)
        {
            var services = messageTypes.Services;

            services.AddSingleton<IContestStore, InMemoryContestStore>();
            services.AddTransient<ICharacterStateService, CharacterStateServiceAdapter>();
            services.AddProfileApiClients();

            services
                .AddTransient<IConnectHook, ConnectHook>()
                .UseUpdateFactory<ContestUpdateFactory>()
                .RegisterHandler<JoinContest, JoinContestHandler>()
                .RegisterHandler<StartContest, StartContestHandler>()
                .RegisterHandler<PullRope, PullRopeHandler>();

            services.AddTransient<IStartupAction, BrokerSubscriptionStartupAction>();

            messageTypes.AddRopeWarMessages();
            return messageTypes;
        }

        public static MessageTypeCacheBuilder AddRopeWarMessages(this MessageTypeCacheBuilder builder)
        {
            return builder.AddMessageTypesFromAssembly(typeof(JoinContest).Assembly);
        }
    }

    // TODO: Remove dependency (reference) to TypingRealm.Hosting after moving IStartupAction to some other assembly.
    public sealed class BrokerSubscriptionStartupAction : IStartupAction
    {
        // TODO: Remove dependency (reference) to TypingRealm.Messaging.Broker after refactoring IBrokerConnectionListener to separate subscriber.
        private readonly IBrokerConnectionListener _broker;

        public BrokerSubscriptionStartupAction(IBrokerConnectionListener broker)
        {
            _broker = broker;
        }

        public ValueTask RunAync(CancellationToken cancellationToken)
        {
            return _broker.SubscribeToAsync<TestMessage>("test", HandleAsync, cancellationToken);
        }

        private ValueTask HandleAsync(TestMessage message)
        {
            Console.WriteLine($"Received test message: {message.Data}.");
            return default;
        }
    }

    public record TestMessage(string Data);
}
