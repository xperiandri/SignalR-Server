using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.CompatTests.Server
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR(options =>
            {
                options.Hubs.EnableDetailedErrors = true;
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            app.UseWebSockets();
            app.UseSignalR();
            app.UseStaticFiles();

            var data = Encoding.UTF8.GetBytes("Server online");
            app.Use(async (context, next) =>
            {
                await context.Response.Body.WriteAsync(data, 0, data.Length);
            });
        }
    }
}
