using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PrintServer
{
    public static class ProcessExtensions
    {
        public static async Task WaitForExitAsync(this Process proc, CancellationToken ct, int checkTimeout = 100)
        {
            await Task.Yield();

            while (true)
            {
                bool status = proc.WaitForExit(checkTimeout);

                if (status)
                {
                    return;
                }
                else if (ct.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }
            }
        }
    }
}
