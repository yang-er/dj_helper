using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PrintServer
{
    public class WebMiddleware : IMiddleware
    {
        ILogger<WebMiddleware> Logger { get; }

        public WebMiddleware(ILogger<WebMiddleware> logger)
        {
            Logger = logger;
        }

        private async Task GetAsync(HttpContext context)
        {
            await context.Response.WriteAsync("<html><body>");

            if (context.Request.Path.Value.Contains("switch"))
            {
                bool cur = QueueService.Instance.Automatic;
                await context.Response.WriteAsync($"<p>Automatic options from {cur} to {!cur}.</p>");
                await context.Response.WriteAsync($"<a href=\"/\">Back</a>");
                QueueService.Instance.Automatic = !cur;
            }
            else
            {
                await context.Response.WriteAsync($"<p>Printing Service running, {QueueService.Instance.Count} queued...</p>");
                await context.Response.WriteAsync($"<a href=\"switch\">Switch Automatic Status, Now {QueueService.Instance.Automatic}</a>");
            }

            await context.Response.WriteAsync("</body></html>");
        }

        private async Task PostAsync(HttpContext context)
        {
            using (var sr = new StreamReader(context.Request.Body))
            {
                var content = await sr.ReadToEndAsync();
                QueueService.Instance.Enqueue(content);
            }
        }

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Request.Method == "GET")
            {
                return GetAsync(context);
            }
            else if (context.Request.Method == "POST")
            {
                return PostAsync(context);
            }
            else
            {
                context.Response.StatusCode = 400;
                return Task.CompletedTask;
            }
        }
    }
}
