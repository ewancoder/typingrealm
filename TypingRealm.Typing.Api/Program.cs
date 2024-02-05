using Google.Apis.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json.Serialization;
using TypingRealm.Typing.DataAccess;
[assembly: ApiController]

var builder = WebApplication.CreateBuilder(args);
const string GoogleClient = "400839590162-24pngke3ov8rbi2f3forabpaufaosldg.apps.googleusercontent.com";

builder.Services.AddCors(o =>
{
    o.AddPolicy(name: "cors", policy =>
    {
        policy.WithOrigins("https://typingrealm.com", "http://localhost:4200")
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
builder.Services.AddScoped<ITypingRepository, TypingRepository>();

var app = builder.Build();

app.UseCors("cors");
app.MapControllers();

await app.RunAsync();

public sealed class AuthenticationContext : IAuthenticationContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthenticationContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetUserProfileId()
    {
        return _httpContextAccessor.HttpContext?.User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value
            ?? throw new InvalidOperationException("User is not authenticated.");
    }
}
