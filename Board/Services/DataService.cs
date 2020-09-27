using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Board.Services
{
    public class DataService : BackgroundService
    {
        public static DataService Instance { get; private set; }

        private ILogger<DataService> Logger { get; }

        internal readonly ILoggerFactory _loggerFactory;

        private readonly Dictionary<string, DataHolder> _dict;

        public DataHolder this[string name] => _dict.GetValueOrDefault(name) ?? new DataHolder(name, "Not Found", _loggerFactory);

        public DataService(ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger<DataService>();
            _loggerFactory = loggerFactory;
            _dict = new Dictionary<string, DataHolder>();
            Instance = this;
            Fetchers = new List<HdojFetcher>();
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
            return _dict.Values.Select(a => new Tenant { name = a._name, title = a._title, hdoj = a._hdoj }).ToList();
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

        public readonly List<HdojFetcher> Fetchers;

        public class Tenant
        {
            public string name { get; set; }
            public string title { get; set; }
            public string hdoj { get; set; }
        }

        private static string ToJson(object value)
        {
            var json = new JsonSerializer();
            var sb = new StringBuilder();
            json.Serialize(new JsonTextWriter(new StringWriter(sb)), value);
            return sb.ToString();
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
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

                    if (tenant.hdoj != null)
                    {
                        dataHolder._hdoj = tenant.hdoj;
                        var st = tenant.hdoj.Split(";");
                        var cid = int.Parse(st[0]);
                        var qot = int.Parse(st[3]);
                        Fetchers.Add(new HdojFetcher(cid, dataHolder, st[1], st[2], qot, _loggerFactory.CreateLogger("HdojFetcher." + tenant.name)));
                    }
                }
            }

            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);

            var tenants = _dict.Values.ToList();
            foreach (var tenant in tenants)
            {
                await tenant.StopAsync();
            }

            var items = tenants.Select(a => new Tenant { name = a._name, title = a._title, hdoj = a._hdoj }).ToList();
            await File.WriteAllTextAsync("tenant.json", ToJson(items));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.WhenAll(Fetchers.Select(a => a.Work()));
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Unknown error.");
                }

                await Task.Delay(30 * 1000, stoppingToken);
            }
        }
    }
}
