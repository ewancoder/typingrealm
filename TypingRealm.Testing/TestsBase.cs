﻿using System;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.Kernel;
using TypingRealm.Messaging;
using Xunit;

namespace TypingRealm.Testing
{
    public abstract class TestsBase : IDisposable
    {
        // Used for creating instances with custom ISpecimenBuilder.
        private readonly object _lock = new object();

        protected Fixture Fixture { get; } = AutoMoqDataAttribute.CreateFixture();
        protected CancellationTokenSource Cts { get; } = new CancellationTokenSource();

        protected T Create<T>() => Fixture.Create<T>();
        protected T Create<T>(params ISpecimenBuilder[] builders)
        {
            lock (_lock)
            {
                foreach (var builder in builders)
                {
                    Fixture.Customizations.Add(builder);
                }

                var result = Fixture.Create<T>();

                foreach (var builder in builders)
                {
                    Fixture.Customizations.Remove(builder);
                }

                return result;
            }
        }

        protected void AssertSerializable<T>()
        {
            // This will use default parameterless constructor and assign all properties.
            var message = Create<T>();

            // This will throw if there's no default parameterless constructor.
            var result = JsonSerializer.Deserialize<T>(
                JsonSerializer.Serialize(message));

            AssertRecursivePropertiesEqual(message!, result!);
        }

        private void AssertRecursivePropertiesEqual(object expectedMessage, object actualMessage)
        {
            foreach (var property in expectedMessage.GetType().GetProperties())
            {
                var propertyType = property.PropertyType;

                if (propertyType.GetCustomAttribute<MessageAttribute>() != null)
                {
                    AssertRecursivePropertiesEqual(
                        property.GetValue(expectedMessage)!,
                        property.GetValue(actualMessage)!);
                }
            }
        }

        protected ValueTask Wait() => Wait(100);
        protected async ValueTask Wait(int milliseconds)
            => await Task.Delay(milliseconds).ConfigureAwait(false);

        protected object? GetPrivateField(object instance, string fieldName)
            => instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance);

        protected async ValueTask<TException> AssertThrowsAsync<TException>(Func<Task> taskFactory, TException exception)
            where TException : Exception
        {
            var thrown = await Assert.ThrowsAsync<TException>(() => taskFactory()).ConfigureAwait(false);
            Assert.Equal(exception, thrown);

            return thrown;
        }

        protected async ValueTask<TException> AssertThrowsAsync<TException>(Func<ValueTask> vtFactory, TException exception)
            where TException : Exception
        {
            var thrown = await Assert.ThrowsAsync<TException>(() => vtFactory().AsTask()).ConfigureAwait(false);
            Assert.Equal(exception, thrown);

            return thrown;
        }

        protected async ValueTask<TException> AssertThrowsAsync<TException>(Func<ValueTask> vtFactory)
            where TException : Exception
        {
            return await Assert.ThrowsAsync<TException>(() => vtFactory().AsTask()).ConfigureAwait(false);
        }

        protected Task<Exception> SwallowAnyAsync(Task task)
            => Assert.ThrowsAnyAsync<Exception>(() => task);

        public void Dispose()
        {
            Cts.Cancel();
            Cts.Dispose();
        }
    }
}
