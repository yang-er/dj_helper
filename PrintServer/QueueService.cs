using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PrintServer
{
    public class QueueService : BackgroundService
    {
        readonly SemaphoreSlim semaphore;
        readonly Queue<string> printing;
        readonly IPrinter printer;

        ILogger<QueueService> Logger { get; }

        public static QueueService Instance { get; private set; }

        public int Count => printing.Count;

        public QueueService(ILogger<QueueService> logger, IPrinter prn)
        {
            semaphore = new SemaphoreSlim(0);
            printing = new Queue<string>();
            Instance = this;
            Logger = logger;
            printer = prn;
        }

        public void Enqueue(string val)
        {
            printing.Enqueue(val);
            semaphore.Release();
        }

        private async Task PrintAsync(string val, CancellationToken stoppingToken)
        {
            try
            {
                await printer.ExecuteAsync(val, stoppingToken);
            }
            catch (Exception ex)
            {
                var errorFile = $"{Guid.NewGuid()}.ps";
                await File.WriteAllTextAsync(errorFile, val);
                Logger.LogError(ex, $"Print failed. Content saved into {errorFile}.");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await semaphore.WaitAsync(stoppingToken);
                    Logger.LogInformation("One task will be printed...");
                    await PrintAsync(printing.Dequeue(), stoppingToken);
                }
            }
            finally
            {
                while (printing.Count > 0)
                {
                    var errorFile = $"{Guid.NewGuid()}.ps";
                    Logger.LogWarning($"Queued content saved into {errorFile}.");
                    await File.WriteAllTextAsync(errorFile, printing.Dequeue());
                }
            }
        }
    }
}
