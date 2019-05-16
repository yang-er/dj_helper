using System.Threading;
using System.Threading.Tasks;

namespace PrintServer
{
    public interface IPrinter
    {
        Task ExecuteAsync(string content, CancellationToken stoppingToken);
    }
}
