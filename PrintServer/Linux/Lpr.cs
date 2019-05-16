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
            // write content into disk.
            var taskGuid = Guid.NewGuid();
            var fileName = $"{taskGuid}.ps";
            await File.WriteAllTextAsync(fileName, content, stoppingToken);

            // send task to printer by LPR command.
            var psi = new ProcessStartInfo();
            psi.FileName = "/usr/bin/lpr";
            psi.Arguments = fileName;
            await Process.Start(psi).WaitForExitAsync(stoppingToken);

            Logger.LogInformation(fileName + " sent to lpr.");

            // wait for empty of System Printing Queue.
            while (!stoppingToken.IsCancellationRequested)
            {
                var psi2 = new ProcessStartInfo();
                psi2.FileName = "/usr/bin/lpq";
                if (psi2.Environment.ContainsKey("LANG"))
                    psi2.Environment["LANG"] = "en-US";
                else psi2.Environment.Add("LANG", "en-US");
                psi2.RedirectStandardOutput = true;
                var proc2 = Process.Start(psi2);
                var ct = await proc2.StandardOutput.ReadToEndAsync();
                if (ct.Contains("no entries")) break;
                Logger.LogWarning("Printer Queue not empty, pending...");
                await Task.Delay(3000, stoppingToken);
            }

            // mark the file as succeeded.
            File.Move(fileName, "@success_" + fileName);
        }
    }
}
