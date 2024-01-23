using Google.Apis.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;
using TypingRealm.Typing.Api.Controllers;
[assembly: ApiController]

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddAuthentication()
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters.ValidIssuer = "https://accounts.google.com";
        o.TokenValidationParameters.ValidAudience = "400839590162-24pngke3ov8rbi2f3forabpaufaosldg.apps.googleusercontent.com";
        o.TokenValidationParameters.SignatureValidator = delegate (string token, TokenValidationParameters parameters)
        {
            var payload = GoogleJsonWebSignature.ValidateAsync(token, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { "400839590162-24pngke3ov8rbi2f3forabpaufaosldg.apps.googleusercontent.com" }
            }).Result;

            return new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token);
        };
    });

builder.Services.AddSingleton(new Dictionary<string, TypingResultDao>());
builder.Services.AddRouting(o => o.LowercaseUrls = true);

var app = builder.Build();

app.MapControllers();

await app.RunAsync();
