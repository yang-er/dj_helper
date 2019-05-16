using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PrintServer.Windows
{
    public class Xpswrite : IPrinter
    {
        ILogger<Xpswrite> Logger { get; }

        public Xpswrite(ILogger<Xpswrite> logger)
        {
            Logger = logger;
        }

        private void PostScript2Xps(Guid guid)
        {
            var param = "-dQUIET -dNOSAFER -r300 -dBATCH " +
                    "-sDEVICE=xpswrite -dNOPAUSE -dNOPROMPT " +
                    $"-sOutputFile={guid}.xps {guid}.ps";

            Logger.LogInformation("gswin64c.exe " + param);
            var psi = new ProcessStartInfo("gswin64c.exe", param);
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;

            var stderr = new StringBuilder();
            var stdout = new StringBuilder();
            var proc = Process.Start(psi);
            proc.OutputDataReceived += (s, e) => stdout.AppendLine(e.Data);
            proc.OutputDataReceived += (s, e) => stderr.AppendLine(e.Data);
            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();

            proc.WaitForExit();
            if (proc.ExitCode != 0)
            {
                var err = stderr.ToString().Trim();
                var ou = stdout.ToString().Trim();
                if (ou != "") throw new Exception(ou);
            }
        }

        public async Task ExecuteAsync(string content, CancellationToken stoppingToken)
        {
            var guid = Guid.NewGuid();
            await File.WriteAllTextAsync($"{guid}.ps", content, Encoding.ASCII);
            PostScript2Xps(guid);
            await Process.Start("xpsrchvw.exe", $"{guid}.xps /p").WaitForExitAsync(stoppingToken);
            File.Move($"{guid}.ps", $"@success_{guid}.ps");
            File.Delete($"{guid}.xps");
        }
    }
}
