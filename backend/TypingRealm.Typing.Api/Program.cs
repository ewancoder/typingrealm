using Microsoft.AspNetCore.Mvc;
using TypingRealm.ApiHost;
using TypingRealm.Typing.DataAccess;
[assembly: ApiController]

var builder = ApiHostBuilder.CreateBuilder();
builder.Services.AddTransient<ITypingRepository, TypingRepository>();
builder.Services.AddSingleton(Random.Shared);

await builder.Build().RunAsync();
