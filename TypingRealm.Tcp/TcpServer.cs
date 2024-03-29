﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TypingRealm.Messaging;
using TypingRealm.Messaging.Connections;
using TypingRealm.Messaging.Serialization.Protobuf;

namespace TypingRealm.Tcp;

public sealed class TcpServer : AsyncManagedDisposable
{
    private readonly ILogger<TcpServer> _logger;
    private readonly IScopedConnectionHandler _connectionHandler;
    private readonly IProtobufConnectionFactory _protobufConnectionFactory;
    private readonly TcpListener _tcpListener;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly List<Task> _connectionProcessors = new List<Task>();
    private Task? _listeningProcess;
    private bool _isStopped;

    public TcpServer(
        int port,
        ILogger<TcpServer> logger,
        IScopedConnectionHandler connectionHandler,
        IProtobufConnectionFactory protobufConnectionFactory)
    {
        _logger = logger;
        _connectionHandler = connectionHandler;
        _protobufConnectionFactory = protobufConnectionFactory;
        _tcpListener = new TcpListener(IPAddress.Parse("0.0.0.0"), port);
    }

    public void Start()
    {
        ThrowIfDisposed();

        if (_listeningProcess != null)
            throw new InvalidOperationException("Server has already started.");

        _tcpListener.Start();
        _listeningProcess = ProcessAsync();
    }

    public ValueTask StopAsync()
    {
        ThrowIfDisposed();

        if (_listeningProcess == null)
            throw new InvalidOperationException("Server has not started yet.");

        return DisposeAsync();
    }

    protected override async ValueTask DisposeManagedResourcesAsync()
    {
        if (_listeningProcess == null)
        {
            _cts.Dispose();
            return;
        }

        // Indicates that no more connections should be accepted.
        // Used solely in ProcessAsync() method to escape while loop.
        _isStopped = true;

        _cts.Cancel();

        await _listeningProcess
            .HandleCancellationAsync(exception =>
            {
                _logger.LogDebug(exception, "Cancellation request received for listening process. Stopped listening for incoming connections.");
            })
            .ConfigureAwait(false);

        _listeningProcess = null;

        // Each connection processor is responsible for handling it's own exceptions.
        // They should all gracefully complete.
        await Task.WhenAll(_connectionProcessors).ConfigureAwait(false);

        _tcpListener.Stop();
        _connectionProcessors.Clear();
        _cts.Dispose();
    }

    /// <summary>
    /// This task never ends until it is stopped using StopAsync method.
    /// </summary>
    private async Task ProcessAsync()
    {
        while (!_isStopped)
        {
            var tcpClient = await _tcpListener.AcceptTcpClientAsync()
                .WithCancellationAsync(_cts.Token)
                .ConfigureAwait(false);

            _cts.Token.ThrowIfCancellationRequested();

            StartHandling(tcpClient);
        }
    }

    private void StartHandling(TcpClient tcpClient)
        => _ = HandleAsync(tcpClient);

    private async Task HandleAsync(TcpClient tcpClient)
    {
        string connectionDetails;

        try
        {
            connectionDetails = tcpClient.Client.RemoteEndPoint?.ToString() ?? "No details";
        }
        catch (Exception exception)
        {
            connectionDetails = "Failed to get details";
            _logger.LogError(exception, "Failed to get connection details.");
        }

        try
        {
            using var stream = tcpClient.GetStream();
            using var sendLock = new SemaphoreSlimLock();
            using var receiveLock = new SemaphoreSlimLock();
            var connection = _protobufConnectionFactory.CreateProtobufConnection(stream)
                .WithLocking(sendLock, receiveLock);

            var task = _connectionHandler
                .HandleAsync(connection, _cts.Token)
                .HandleCancellationAsync(exception =>
                {
                    _logger.LogDebug(
                        exception,
                        "Cancellation request received for client: {ConnectionDetails}",
                        connectionDetails);
                })
                .HandleExceptionAsync<Exception>(exception =>
                {
                    _logger.LogError(
                        exception,
                        "Error happened while handling TCP connection: {ConnectionDetails}",
                        connectionDetails);
                });

            _connectionProcessors.Add(task);
            _connectionProcessors.RemoveAll(t => t.IsCompleted);

            await task.ConfigureAwait(false);
        }
#pragma warning disable CA1031 // This method should not throw ANY exceptions, it is a top-level handler.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            _logger.LogError(
                exception,
                "Error happened while creating TCP connection: {ConnectionDetails}",
                connectionDetails);
        }
        finally
        {
            tcpClient.Dispose();
        }
    }
}
