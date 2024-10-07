using Microsoft.AspNetCore.Mvc;
using TypingRealm.ApiHost;
[assembly: ApiController]

var builder = ApiHostBuilder.CreateBuilder();
await builder.Build().RunAsync();
