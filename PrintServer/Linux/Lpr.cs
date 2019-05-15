using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PrintServer.Linux
{
    public class Lpr : IPrinter
    {
        ILogger<Lpr> Logger { get; }

        public Lpr(ILogger<Lpr> logger)
        {
            Logger = logger;
        }

        public async Task ExecuteAsync(string content, CancellationToken stoppingToken)
        {
            var fileName = $"{Guid.NewGuid()}.ps";
            await File.WriteAllTextAsync(fileName, content, stoppingToken);
            var psi = new ProcessStartInfo();
            psi.FileName = "/usr/bin/lpr";
            psi.Arguments = fileName;
            var proc = Process.Start(psi);

            while (!stoppingToken.IsCancellationRequested)
                if (proc.WaitForExit(100))
                    break;

            Logger.LogInformation(fileName + " sent to lpr.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var psi2 = new ProcessStartInfo();
                psi2.FileName = "/usr/bin/lpq";
                psi2.RedirectStandardOutput = true;
                var proc2 = Process.Start(psi2);
                var ct = await proc2.StandardOutput.ReadToEndAsync();
                if (ct.Contains("no entries")) break;
                Logger.LogWarning("Printer Queue not empty, pending...");
                await Task.Delay(3000, stoppingToken);
            }
        }
    }
}
