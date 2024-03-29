﻿using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Elasticsearch;
using Serilog.Sinks.File;
using TypingRealm.Configuration;

namespace TypingRealm.Logging;

// TODO: This is not unit tested, hard to unit test.
public static class RegistrationExtensions
{
    public static ILoggingBuilder AddTyrLogging(
        this ILoggingBuilder builder, IConfiguration configuration)
    {
        builder.ClearProviders();
        builder.AddSerilog(CreateSerilogLogger(configuration));

        return builder;
    }

    private static Serilog.ILogger CreateSerilogLogger(IConfiguration configuration)
    {
        var isDevelopment = DebugHelpers.IsDevelopment();
        var environment = DebugHelpers.GetEnvironment().ToLowerInvariant();
        var serviceId = configuration.GetServiceId();
        var indexFormat = $"typingrealm-logs-{environment}-{serviceId}-{{0:yyyy.MM}}";

        var connectionString = configuration.GetConnectionString("Logging");

        var config = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("TypingRealm", isDevelopment ? LogEventLevel.Verbose : LogEventLevel.Debug)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Environment", environment)
            .Enrich.WithProperty("ServiceId", serviceId)
            .WriteTo.Console()
            .WriteTo.File(new CompactJsonFormatter(), "logs/log", rollingInterval: RollingInterval.Day);

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var parts = connectionString.Split(";");
            var url = parts[0];
            var password = parts[1];

            config = config.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(url))
            {
                IndexFormat = indexFormat,
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                ModifyConnectionSettings = config => config.BasicAuthentication("elastic", password),
                EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                                   EmitEventFailureHandling.WriteToFailureSink,
                FailureSink = new FileSink("logs/elastic-failures", new CompactJsonFormatter(), null)

                // TODO:
                // For some reason when using it with buffer - some logs are not being written.
                // So right now it's dependent on up and running elastic stack.
                // Improve this so that we use filebeat/logstash or smth like that.
                //BufferBaseFilename = "./logs/elastic-buffer"
            });
        }

        return config.CreateLogger();
    }
}
