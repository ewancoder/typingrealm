using System;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace TypingRealm.ApiHost;

public sealed record DiagnosticsData(
    string Instance,
    DateTime Now,
    int Count);

public sealed class ApiHostStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseCors("cors");

            // The following should go in that order, instead of MapControllers() in the host.
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                var count = 0;
                var instance = Guid.NewGuid().ToString();
                endpoints.MapControllers();
                endpoints.MapGet("/diag", async context =>
                {
                    var content = JsonSerializer.Serialize(
                        new DiagnosticsData(instance, DateTime.Now, count++));

                    await context.Response.WriteAsync(content)
                        .ConfigureAwait(false);
                });
            });

            next(app);
        };
    }
}
