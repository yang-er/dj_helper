using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Board.Services
{
    public class DataService : IHostedService
    {
        public static DataService Instance { get; private set; }

        private ILogger<DataService> Logger { get; }

        private readonly ILoggerFactory _loggerFactory;

        private readonly ConcurrentDictionary<string, DataHolder> _dict;

        public DataHolder this[string name] => _dict.GetValueOrDefault(name) ?? new DataHolder(name, "Not Found", _loggerFactory);

        public DataService(ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger<DataService>();
            _loggerFactory = loggerFactory;
            _dict = new ConcurrentDictionary<string, DataHolder>();
            Instance = this;
        }

        public bool Have(string s)
        {
            return _dict.ContainsKey(s);
        }

        public void Create(string name, string title)
        {
            _dict.TryAdd(name, new DataHolder(name, title, _loggerFactory));
        }

        public List<Tenant> GetTenants()
        {
            return _dict.Values.Select(a => new Tenant { name = a._name, title = a._title }).ToList();
        }

        private static T Parse<T>(string jsonString)
        {
            try
            {
                var json = new JsonSerializer();
                if (jsonString == "") throw new JsonReaderException();
                return json.Deserialize<T>(new JsonTextReader(new StringReader(jsonString)));
            }
            catch (JsonException)
            {
                return default(T);
            }
        }

        public class Tenant
        {
            public string name { get; set; }
            public string title { get; set; }
        }

        private static string ToJson(object value)
        {
            var json = new JsonSerializer();
            var sb = new StringBuilder();
            json.Serialize(new JsonTextWriter(new StringWriter(sb)), value);
            return sb.ToString();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (File.Exists("tenant.json"))
            {
                var content = await File.ReadAllTextAsync("tenant.json");
                var tenants = Parse<List<Tenant>>(content);
                Logger.LogInformation("tenant.json cache loaded from disk.");

                foreach (var tenant in tenants)
                {
                    var dataHolder = new DataHolder(tenant.name, tenant.title, _loggerFactory);
                    await dataHolder.StartAsync();
                    _dict.TryAdd(tenant.name, dataHolder);
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            var tenants = _dict.Values.ToList();
            foreach (var tenant in tenants)
            {
                await tenant.StopAsync();
            }

            var items = tenants.Select(a => new Tenant { name = a._name, title = a._title }).ToList();
            await File.WriteAllTextAsync("tenant.json", ToJson(items));
        }
    }
}
