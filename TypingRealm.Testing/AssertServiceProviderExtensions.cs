﻿using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace TypingRealm.Testing;

public static class AssertServiceProviderExtensions
{
    public static object AssertRegistered(
        this IServiceProvider provider,
        Type interfaceType,
        Type implementationType)
    {
        var implementation = provider.GetRequiredService(interfaceType);
        Assert.NotNull(implementation);
        Assert.IsType(implementationType, implementation);

        return implementation;
    }

    public static object AssertRegisteredTransient(
        this IServiceProvider provider,
        Type interfaceType,
        Type implementationType)
    {
        var i1 = provider.AssertRegistered(interfaceType, implementationType);
        var i2 = provider.AssertRegistered(interfaceType, implementationType);

        Assert.NotEqual(i1, i2);

        return i1;
    }

    public static object AssertRegisteredSingleton(
        this IServiceProvider provider,
        Type interfaceType,
        Type implementationType)
    {
        var i1 = provider.AssertRegistered(interfaceType, implementationType);
        var i2 = provider.AssertRegistered(interfaceType, implementationType);

        using var scope1 = provider.CreateScope();
        var i3 = scope1.ServiceProvider.AssertRegistered(interfaceType, implementationType);

        Assert.Equal(i1, i2);
        Assert.Equal(i1, i3);

        return i1;
    }

    public static object AssertRegisteredScoped(
        this IServiceProvider provider,
        Type interfaceType,
        Type implementationType)
    {
        var i1 = provider.AssertRegistered(interfaceType, implementationType);
        var i2 = provider.AssertRegistered(interfaceType, implementationType);

        using var scope1 = provider.CreateScope();
        var i3 = scope1.ServiceProvider.AssertRegistered(interfaceType, implementationType);

        using var scope2 = provider.CreateScope();
        var i4 = scope2.ServiceProvider.AssertRegistered(interfaceType, implementationType);

        Assert.Equal(i1, i2);
        Assert.NotEqual(i1, i3);
        Assert.NotEqual(i1, i4);
        Assert.NotEqual(i3, i4);

        return i1;
    }

    public static TImplementation AssertRegistered<TInterface, TImplementation>(
        this IServiceProvider provider, ServiceLifetime lifetime) => lifetime switch
        {
            ServiceLifetime.Transient => AssertRegisteredTransient<TInterface, TImplementation>(provider),
            ServiceLifetime.Scoped => AssertRegisteredScoped<TInterface, TImplementation>(provider),
            ServiceLifetime.Singleton => AssertRegisteredSingleton<TInterface, TImplementation>(provider),
            _ => throw new InvalidOperationException()
        };

    public static TImplementation AssertRegistered<TInterface, TImplementation>(this IServiceProvider provider)
        => (TImplementation)provider.AssertRegistered(typeof(TInterface), typeof(TImplementation));

    public static TImplementation AssertRegisteredTransient<TInterface, TImplementation>(this IServiceProvider provider)
        => (TImplementation)provider.AssertRegisteredTransient(typeof(TInterface), typeof(TImplementation));

    public static TImplementation AssertRegisteredSingleton<TInterface, TImplementation>(this IServiceProvider provider)
        => (TImplementation)provider.AssertRegisteredSingleton(typeof(TInterface), typeof(TImplementation));

    public static TImplementation AssertRegisteredScoped<TInterface, TImplementation>(this IServiceProvider provider)
        => (TImplementation)provider.AssertRegisteredScoped(typeof(TInterface), typeof(TImplementation));
}
