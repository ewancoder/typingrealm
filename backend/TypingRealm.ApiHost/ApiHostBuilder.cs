using System.Text.Json.Serialization;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TypingRealm.Framework;

namespace TypingRealm.ApiHost;

public static partial class ApiHostBuilder
{
    public static WebApplicationBuilder CreateBuilder()
    {
        var builder = WebApplication.CreateBuilder();
        const string GoogleClient = "400839590162-24pngke3ov8rbi2f3forabpaufaosldg.apps.googleusercontent.com";

        builder.Services.AddCors(o =>
        {
            o.AddPolicy(name: "cors", policy =>
            {
                // TODO: Only allow production to accept calls from production.
                // TODO: Also create google cloud credentials separately for production/dev.
                policy.WithOrigins(
                    "https://typingrealm.com",
                    "https://game.typingrealm.com",
                    "http://localhost:4200",
                    "https://localhost",
                    "https://dev.typingrealm.com",
                    "https://dev.game.typingrealm.com")
                    .AllowAnyMethod() // TODO: allow only specific methods; this was done because delete wasn't allowed by default
                    .WithHeaders("Authorization", "Content-Type");
            });
        });

        builder.Services.AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        builder.Services.AddAuthentication()
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters.ValidIssuer = "https://accounts.google.com";
                o.TokenValidationParameters.ValidAudience = GoogleClient;
#if DEBUG
                o.TokenValidationParameters.ValidateLifetime = false;
#endif
                o.TokenValidationParameters.SignatureValidator = delegate (string token, TokenValidationParameters parameters)
                {
                    GoogleJsonWebSignature.ValidateAsync(token, new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = [GoogleClient]
                    });

                    return new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token);
                };
            });

        builder.Services.AddRouting(o => o.LowercaseUrls = true);
        builder.Services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();
        builder.Services.AddTransient<IAuthenticationContext, AuthenticationContext>();
        builder.Services.AddTransient<IStartupFilter, ApiHostStartupFilter>();

        return builder;
    }
}
