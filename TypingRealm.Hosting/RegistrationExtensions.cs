using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using TypingRealm.Authentication;
using TypingRealm.Communication;
using TypingRealm.Messaging;
using TypingRealm.Messaging.Broker.Client;
using TypingRealm.Messaging.Broker.Tcp;
using TypingRealm.Messaging.Serialization;
using TypingRealm.Messaging.Serialization.Json;
using TypingRealm.Messaging.Serialization.Protobuf;
using TypingRealm.SignalR;
using TypingRealm.Tcp;

namespace TypingRealm.Hosting
{
    public static class RegistrationExtensions
    {
        private const string CorsPolicyName = "CorsPolicy";
        private static readonly string[] _corsAllowedOrigins = new[]
        {
            "http://localhost:4200",
            "https://localhost:4200",
            "http://typingrealm.com:4200",
            "https://typingrealm.com:4200"
        };

        public static MessageTypeCacheBuilder UseTcpHost(this IServiceCollection services, int port)
        {
            services
                .AddProtobuf()
                .AddTcpServer(port);

            services
                .AddCommunication()
                .RegisterMessaging();
            var builder = services.AddSerializationCore();

            builder
                .AddTyrServiceWithoutAspNetAuthentication()
                .UseLocalProvider();

            builder.AddBrokerMessaging();

            services.AddHostedService<TcpServerHostedService>();

            services.AddHostedService<StartupActionRunner>();

            return builder;
        }

        public static MessageTypeCacheBuilder UseSignalRHost(this IServiceCollection services)
        {
            services.AddSignalR();

            services.AddCors(options => options.AddPolicy(
                CorsPolicyName,
                builder => builder
                    .WithOrigins(_corsAllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()));

            services
                .RegisterMessageHub();

            services
                .AddCommunication()
                .RegisterMessaging();
            var builder = services.AddSerializationCore();
            builder
                .AddJson()
                .AddTyrWebServiceAuthentication()
                .UseLocalProvider();

            services.AddProtobuf(); // We need it for TcpBrokeredMessaging :(
            builder.AddBrokerMessaging();

            services.AddTransient<IStartupFilter, SignalRStartupFilter>();

            services.AddHostedService<StartupActionRunner>();

            return builder;
        }

        public static IServiceCollection UseWebApiHost(this IServiceCollection services, Assembly controllersAssembly)
        {
            services.AddCors(options => options.AddPolicy(
                CorsPolicyName,
                builder => builder
                    .WithOrigins(_corsAllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()));

            services.AddCommunication();
            services.AddTyrApiAuthentication()
                .UseLocalProvider();

            services.AddControllers()
                .PartManager.ApplicationParts.Add(new AssemblyPart(controllersAssembly));

            services.AddTransient<IStartupFilter, WebApiStartupFilter>();

            services.AddHostedService<StartupActionRunner>();

            return services;
        }

        private static MessageTypeCacheBuilder AddBrokerMessaging(this MessageTypeCacheBuilder messageTypes)
        {
            var services = messageTypes.Services;

            services.AddTcpBrokerConnection();
            messageTypes.AddMessagingBrokerClient();

            services.AddSingleton<IStartupAction, ListenToMessageBrokerStartupAction>();

            return messageTypes;
        }
    }
}
