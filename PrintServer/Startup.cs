using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PrintServer
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                services.AddSingleton<IPrinter, Linux.Lpr>();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                services.AddSingleton<IPrinter, Windows.Xpswrite>();
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            services.AddHostedService<QueueService>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Run(async (context) =>
            {
                if (context.Request.Method == "GET")
                {
                    await context.Response.WriteAsync($"Printing Service running, {QueueService.Instance.Count} queued...");
                }
                else if (context.Request.Method == "POST")
                {
                    using (var sr = new StreamReader(context.Request.Body))
                    {
                        var content = await sr.ReadToEndAsync();
                        QueueService.Instance.Enqueue(content);
                    }
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            });
        }
    }
}
