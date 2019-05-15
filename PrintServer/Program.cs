using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace PrintServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var builder = WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
            if (args.Length == 1)
                builder.UseUrls("http://*:" + args[0]);
            return builder;
        }
    }
}
