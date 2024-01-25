using Google.Apis.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TypingRealm.Typing.Api.Controllers;
[assembly: ApiController]

var builder = WebApplication.CreateBuilder(args);
const string GoogleClient = "400839590162-24pngke3ov8rbi2f3forabpaufaosldg.apps.googleusercontent.com";

builder.Services.AddCors(o =>
{
    o.AddPolicy(name: "cors", policy =>
    {
        policy.WithOrigins("https://typingrealm.com", "http://localhost:4200")
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

builder.Services.AddSingleton(new Dictionary<string, TypingResultDao>());
builder.Services.AddRouting(o => o.LowercaseUrls = true);

var app = builder.Build();

app.UseCors("cors");
app.MapControllers();

await app.RunAsync();
