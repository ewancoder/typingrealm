﻿using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TypingRealm.Authentication;
using TypingRealm.Authentication.Service;
using TypingRealm.Messaging;
using TypingRealm.Messaging.Serialization;
using TypingRealm.Messaging.Serialization.Json;
using TypingRealm.Messaging.Serialization.Protobuf;
using TypingRealm.SignalR;
using TypingRealm.Tcp;

namespace TypingRealm.Hosting.Service;

public static class RegistrationExtensions
{
    public static MessageTypeCacheBuilder UseTcpHost(this IServiceCollection services, IConfiguration configuration, int port)
    {
        Hosting.RegistrationExtensions.SetupCommonDependencies(services, configuration);
        var builder = SetupCommonMessagingServiceDependencies(services);

        // Authentication.
        builder.AddTyrServiceWithoutAspNetAuthentication();

        // TCP, Protobuf.
        services
            .AddProtobuf() // Already has a call to AddProtobufMessageSerializer.
            .AddTcpServer(port)
            .AddHostedService<TcpServerHostedService>();

        // Use this to serialize/deserialize messages in JSON instead of protobuf base64 string.
        // This can be removed for total Protobuf serialization mode.
        services.UseJsonMessageSerializer();

        return builder;
    }

    public static MessageTypeCacheBuilder UseSignalRHost(this IServiceCollection services, IConfiguration configuration, params Assembly[] controllersAssemblies)
    {
        Hosting.RegistrationExtensions.SetupCommonDependencies(services, configuration);
        Hosting.RegistrationExtensions.SetupCommonAspNetDependencies<SignalRStartupFilter>(
            services,
            configuration,
            controllersAssemblies.Append(typeof(RealtimeAuthenticationController).Assembly).ToArray());

        var builder = SetupCommonMessagingServiceDependencies(services);

        // Authentication.
        builder.AddTyrServiceAuthentication();

        // SignalR.
        services.AddSignalR();
        services.RegisterMessageHub();

        // Message serialization.
        services.UseJsonMessageSerializer();

        // Use this to enable protobuf base64 string message serializer instead of json.
        // This can be removed for total JSON serialization mode.
        // For browsers we obviously want it commented out.
        //services.AddProtobufMessageSerializer();

        return builder;
    }

    private static MessageTypeCacheBuilder SetupCommonMessagingServiceDependencies(IServiceCollection services)
    {
        services.RegisterMessaging();

        var builder = services.AddSerializationCore();

        return builder;
    }
}
