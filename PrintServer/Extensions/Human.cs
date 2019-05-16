using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PrintServer
{
    public class Human : IPrinter
    {
        public async Task ExecuteAsync(string content, CancellationToken stoppingToken)
        {
            await File.WriteAllTextAsync($"@hand_{Guid.NewGuid()}.ps", content, stoppingToken);
        }
    }
}
