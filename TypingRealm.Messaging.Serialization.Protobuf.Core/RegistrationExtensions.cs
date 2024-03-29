﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace TypingRealm.Messaging.Serialization.Protobuf;

public static class RegistrationExtensions
{
    /// <summary>
    /// Registers protobuf stream connection factory.
    /// </summary>
    public static IServiceCollection AddProtobuf(this IServiceCollection services)
    {
        services.AddTransient<IProtobufConnectionFactory, ProtobufConnectionFactory>();
        services.AddSingleton<IProtobufFieldNumberCache, ProtobufFieldNumberCache>();

        services.AddTransient<IProtobufStreamSerializer>(provider =>
        {
            return new ProtobufStreamSerializer(new[]
            {
                    typeof(MessageData),
                    typeof(MessageMetadata),
                    typeof(ClientToServerMessageMetadata) // This is needed so that properties of this class are serialized.
            }, new Dictionary<Type, IEnumerable<Type>>
            {
                [typeof(MessageMetadata)] = new[] { typeof(ClientToServerMessageMetadata) } // This is needed so that Protobuf knows about derived types.
            });
        });

        // Consider removing this call from here so that client is required to pick either Json or Protobuf serializer.
        services.AddProtobufMessageSerializer();

        return services;
    }

    public static IServiceCollection AddProtobufMessageSerializer(this IServiceCollection services)
    {
        // TODO: MAJOR It SHOULD be transient so other types can be registered as well (json after protobuf).
        // But we can register a list of all types as singletone not to calculate it every time.
        // Or change implementation so that it takes the list from cache.
        services.AddTransient<IMessageSerializer>(provider =>
        {
            var cache = provider.GetRequiredService<IMessageTypeCache>();

            return new ProtobufMessageSerializer(cache.GetAllTypes().Select(kv => kv.Value));
        });

        return services;
    }
}
