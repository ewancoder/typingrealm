﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using TypingRealm.Authentication.Api;
using TypingRealm.Communication;
using TypingRealm.Communication.Redis;
using TypingRealm.Configuration;
using TypingRealm.Hosting.Deployment;
using TypingRealm.Hosting.HealthChecks;
using TypingRealm.Serialization;

namespace TypingRealm.Hosting;

public static class RegistrationExtensions
{
    public static readonly string CorsPolicyName = "CorsPolicy";
    public static readonly string[] CorsAllowedOrigins = DebugHelpers.IsDevelopment()
        ? GetDevelopmentAllowedOrigins().ToArray()
        : new[] { "https://typingrealm.com" };

    private static IEnumerable<string> GetDevelopmentAllowedOrigins()
    {
        var hosts = new[]
        {
                "127.0.0.1",
                "localhost",
                "typingrealm.com",
                "dev.typingrealm.com"
            };

        var ports = new[]
        {
                "",
                ":4200"
            };

        foreach (var host in hosts)
        {
            foreach (var port in ports)
            {
                yield return $"http://{host}{port}";
                yield return $"https://{host}{port}";
            }
        }
    }

    public static IServiceCollection UseWebApiHost(
        this IServiceCollection services, IConfiguration configuration, Assembly controllersAssembly)
    {
        SetupCommonDependencies(services, configuration);
        SetupCommonAspNetDependencies<WebApiStartupFilter>(services, configuration, controllersAssembly);

        // Authentication.
        services.AddTyrApiAuthentication();

        return services;
    }

    /// <summary>
    /// Used by Web API, SignalR and TCP hosts (by everything).
    /// Registered before host-specific dependencies are added.
    /// </summary>
    /// <param name="services"></param>
    public static IServiceCollection SetupCommonDependencies(
        IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient();
        services.AddCommunication();
        services.AddSerialization();

        // Technology specific.
        if (DebugHelpers.UseInfrastructure)
        {
            services.TryAddRedisServiceCaching(configuration);
        }

        // Deployment of infrastructure from all hosts.
        services.AddHostedService<InfrastructureDeploymentHostedService>();

        return services;
    }

    /// <summary>
    /// Used by Web API and SignalR hosts that use ASP.Net hosting framework.
    /// Is not used by custom tools / console apps / TCP servers.
    /// </summary>
    public static IServiceCollection SetupCommonAspNetDependencies<TStartupFilter>(
        IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] controllersAssemblies)
        where TStartupFilter : class, IStartupFilter
    {
        services.AddCors(options => options.AddPolicy(
            CorsPolicyName,
            builder => builder
                .WithOrigins(CorsAllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()));

        // TODO: Consider how to add healthchecks for custom TCP hosts (ping address?).
        services.AddTyrHealthChecks();

        // Web API controllers.
        var mvcBuilder = services.AddControllers(options =>
        {
            // This is so Swagger shows up this response type.
            options.Filters.Add(new ProducesAttribute("application/json"));
        });

        mvcBuilder.PartManager.ApplicationParts.Add(new AssemblyPart(typeof(DiagnosticsController).Assembly));

        // If host has custom APIs.
        foreach (var controllersAssembly in controllersAssemblies)
        {
            mvcBuilder.PartManager.ApplicationParts.Add(new AssemblyPart(controllersAssembly));
        }

        mvcBuilder.AddJsonOptions(options =>
        {
            // Accept enum values as strings to Web API endpoints.
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        // Fluent validation.
        mvcBuilder.AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblies(controllersAssemblies));

        // Swagger.
        services.AddSwaggerGen(options =>
        {
            // We need this to solve the issue with PascalCase [FromRoute] parameters when using objects.
            options.DescribeAllParametersInCamelCase();

            options.OrderActionsBy(x => x.RelativePath);

            // Exclude diagnostics /api/diagnostics/* endpoints.
            options.DocInclusionPredicate((_, x) =>
            {
                if (x.RelativePath == null)
                    return true;

                return !x.RelativePath.StartsWith("api/diagnostics", StringComparison.Ordinal);
            });

            // Load description from MD file.
            var description = string.Empty;
            var apiFilePath = Path.Combine(AppContext.BaseDirectory, "Api.md");
            if (File.Exists(apiFilePath))
                description = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Api.md"));

            var serviceId = configuration.GetServiceId();
            var title = $"[ {serviceId} ] API";
            if (description.StartsWith("# "))
            {
                title = $"{description.Split('\n')[0].Replace("# ", "").Replace("\r", "").Trim()} [ {serviceId} ]";
                description = description[(description.IndexOf('\n') + 1)..];
            }

            // v1 is used as a key to swagger json.
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = title,
                Version = "v1", // Shown on swagger page.
                Description = description
            });

            var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml");
            foreach (var xmlFile in xmlFiles)
            {
                options.IncludeXmlComments(xmlFile);
            }
        });

        // Web API or SingnalR or another custom Startup filter.
        services.AddTransient<IStartupFilter, TStartupFilter>();

        return services;
    }
}
