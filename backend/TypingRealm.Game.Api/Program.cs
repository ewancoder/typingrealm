using Microsoft.AspNetCore.Mvc;
using TypingRealm.ApiHost;
using TypingRealm.Game.DataAccess;
[assembly: ApiController]

var builder = ApiHostBuilder.CreateBuilder();

builder.Services.AddGameDataAccess(
    "Host=typingrealm.com;Port=20133;Database=db;Username=postgres;Password=admin");

await builder.Build().RunAsync();
