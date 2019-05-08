using Board.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
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

        public Dictionary<string, TeamModel> Teams { get; private set; }

        private List<TeamModel> _teamsInner;

        public Dictionary<string, AffiliationModel> Organizations { get; private set; }

        private List<AffiliationModel> _affilInner;

        public ScoreBoardModel ScoreBoard { get; private set; }

        public ContestModel Contest { get; private set; }

        public List<ProblemModel> Problems { get; private set; }

        public DateTime BoardFreezeTime { get; private set; }

        public DateTime LastUpdate { get; private set; }

        public int SolvedTotal { get; set; } = 0;

        public int[] Solved { get; set; }

        public int[] Incorrect { get; set; }

        public int[] Pending { get; set; }

        public int[] FirstSolve { get; set; }

        public int FreezeLength { get; private set; }

        public DataService(ILogger<DataService> logger)
        {
            Logger = logger;
            Instance = this;
            Teams = new Dictionary<string, TeamModel>();
            Organizations = new Dictionary<string, AffiliationModel>();
        }

        public void SetScoreboard(ScoreBoardModel sb)
        {
            int n = sb.rows.FirstOrDefault()?.problems?.Length ?? 0;
            var solvedtot = 0;
            var solved = new int[n];
            var inc = new int[n];
            var pend = new int[n];
            var fb = new int[n];

            for (int i = 0; i < n; i++)
                fb[i] = int.MaxValue;

            foreach (var row in sb.rows)
            {
                for (int i = 0; i < n; i++)
                {
                    var prob = row.problems[i];

                    if (prob.solved)
                    {
                        solvedtot++;
                        solved[i]++;
                        inc[i]--;
                        fb[i] = Math.Min(fb[i], prob.time);
                    }

                    inc[i] += prob.num_judged;
                    pend[i] += prob.num_pending;
                }
            }

            ScoreBoard = sb;
            SolvedTotal = solvedtot;
            Solved = solved;
            Incorrect = inc;
            Pending = pend;
            FirstSolve = fb;
            LastUpdate = DateTime.Now;
        }

        public void SetTeams(List<TeamModel> teams)
        {
            _teamsInner = teams;
            Teams = teams.ToDictionary(t => t.id);
        }

        public void SetOrganizations(List<AffiliationModel> affs)
        {
            _affilInner = affs;
            Organizations = affs.ToDictionary(a => a.id);
        }

        public void SetContest(ContestModel cont)
        {
            var sp = TimeSpan.Parse(cont.scoreboard_freeze_duration);
            Contest = cont;
            BoardFreezeTime = cont.end_time - sp;
            FreezeLength = (int)sp.TotalMinutes;
        }

        public void SetProblems(List<ProblemModel> cont)
        {
            Problems = cont;
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

        private static string ToJson(object value)
        {
            var json = new JsonSerializer();
            var sb = new StringBuilder();
            json.Serialize(new JsonTextWriter(new StringWriter(sb)), value);
            return sb.ToString();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            ScoreBoardModel sb = null;
            ContestModel ct = null;
            
            if (File.Exists("scoreboard.json"))
            {
                var content = await File.ReadAllTextAsync("scoreboard.json", cancellationToken);
                sb = Parse<ScoreBoardModel>(content);
                Logger.LogInformation("scoreboard.json cache loaded from disk.");
            }

            if (File.Exists("organizations.json"))
            {
                var content = await File.ReadAllTextAsync("organizations.json", cancellationToken);
                SetOrganizations(Parse<List<AffiliationModel>>(content) ?? new List<AffiliationModel>());
                Logger.LogInformation("organizations.json cache loaded from disk.");
            }

            if (File.Exists("teams.json"))
            {
                var content = await File.ReadAllTextAsync("teams.json", cancellationToken);
                SetTeams(Parse<List<TeamModel>>(content) ?? new List<TeamModel>());
                Logger.LogInformation("teams.json cache loaded from disk.");
            }

            if (File.Exists("problems.json"))
            {
                var content = await File.ReadAllTextAsync("problems.json", cancellationToken);
                Problems = Parse<List<ProblemModel>>(content);
                Logger.LogInformation("problems.json cache loaded from disk.");
            }

            if (File.Exists("contest.json"))
            {
                var content = await File.ReadAllTextAsync("contest.json", cancellationToken);
                ct = Parse<ContestModel>(content);
                Logger.LogInformation("contest.json cache loaded from disk.");
            }

            sb = sb ?? new ScoreBoardModel
            {
                contest_time = "",
                event_id = "",
                rows = new Row[0],
                state = new State(),
                time = DateTime.UnixEpoch
            };

            SetScoreboard(sb);

            Problems = Problems ?? new List<ProblemModel>();

            ct = ct ?? new ContestModel
            {
                formal_name = "Demo contest",
                penalty_time = 20,
                start_time = DateTime.Now.AddDays(-1),
                end_time = DateTime.Now.AddDays(1),
                duration = "48:00:00.000",
                scoreboard_freeze_duration = "1:00:00.000",
                id = "2",
                external_id = "demo",
                name = "Demo contest",
                shortname = "demo",
            };

            SetContest(ct);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await File.WriteAllTextAsync("scoreboard.json", ToJson(ScoreBoard), cancellationToken);
            await File.WriteAllTextAsync("teams.json", ToJson(_teamsInner), cancellationToken);
            await File.WriteAllTextAsync("problems.json", ToJson(Problems), cancellationToken);
            await File.WriteAllTextAsync("organizations.json", ToJson(_affilInner), cancellationToken);
            await File.WriteAllTextAsync("contest.json", ToJson(Contest), cancellationToken);
        }
    }
}
