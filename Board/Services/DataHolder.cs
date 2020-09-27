using Board.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Board.Services
{
    public class DataHolder
    {
        public readonly string _name;

        public readonly string _title;

        public string _hdoj;

        private readonly ILogger Logger;

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

        public DataHolder(string name, string title, ILoggerFactory loggerFactory)
        {
            _name = name;
            _title = title;
            Logger = loggerFactory.CreateLogger("Scoreboard." + name);
            Teams = new Dictionary<string, TeamModel>();
            Organizations = new Dictionary<string, AffiliationModel>();

            SetScoreboard(new ScoreBoardModel
            {
                contest_time = "",
                event_id = "",
                rows = new List<Row>(),
                state = new State(),
                time = DateTime.UnixEpoch
            });

            Problems = new List<ProblemModel>();

            SetContest(new ContestModel
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
            });
        }

        public async Task StartAsync()
        {
            ScoreBoardModel sb = null;
            ContestModel ct = null;

            if (File.Exists(_name + ".scoreboard.json"))
            {
                var content = await File.ReadAllTextAsync(_name + ".scoreboard.json");
                sb = Parse<ScoreBoardModel>(content);
                Logger.LogInformation(_name + ".scoreboard.json cache loaded from disk.");
            }

            if (File.Exists(_name + ".organizations.json"))
            {
                var content = await File.ReadAllTextAsync(_name + ".organizations.json");
                SetOrganizations(Parse<List<AffiliationModel>>(content) ?? new List<AffiliationModel>());
                Logger.LogInformation(_name + ".organizations.json cache loaded from disk.");
            }

            if (File.Exists(_name + ".teams.json"))
            {
                var content = await File.ReadAllTextAsync(_name + ".teams.json");
                SetTeams(Parse<List<TeamModel>>(content) ?? new List<TeamModel>());
                Logger.LogInformation(_name + ".teams.json cache loaded from disk.");
            }

            if (File.Exists(_name + ".problems.json"))
            {
                var content = await File.ReadAllTextAsync(_name + ".problems.json");
                Problems = Parse<List<ProblemModel>>(content);
                Logger.LogInformation(_name + ".problems.json cache loaded from disk.");
            }

            if (File.Exists(_name + ".groups.json"))
            {
                var content = await File.ReadAllTextAsync(_name + ".groups.json");
                SetGroups(Parse<List<Group>>(content));
                Logger.LogInformation(_name + ".groups.json cache loaded from disk.");
            }

            if (File.Exists(_name + ".contest.json"))
            {
                var content = await File.ReadAllTextAsync(_name + ".contest.json");
                ct = Parse<ContestModel>(content);
                Logger.LogInformation(_name + ".contest.json cache loaded from disk.");
            }

            sb ??= new ScoreBoardModel
            {
                contest_time = "",
                event_id = "",
                rows = new List<Row>(),
                state = new State(),
                time = DateTime.UnixEpoch
            };

            SetScoreboard(sb);

            Problems ??= new List<ProblemModel>();

            ct ??= new ContestModel
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

        public async Task StopAsync()
        {
            await File.WriteAllTextAsync(_name + ".scoreboard.json", ToJson(ScoreBoard));
            await File.WriteAllTextAsync(_name + ".teams.json", ToJson(_teamsInner));
            await File.WriteAllTextAsync(_name + ".problems.json", ToJson(Problems));
            await File.WriteAllTextAsync(_name + ".organizations.json", ToJson(_affilInner));
            await File.WriteAllTextAsync(_name + ".contest.json", ToJson(Contest));
            await File.WriteAllTextAsync(_name + ".groups.json", ToJson(_groupInner));
        }

        public Dictionary<string, TeamModel> Teams { get; private set; }

        private List<TeamModel> _teamsInner;

        public Dictionary<string, AffiliationModel> Organizations { get; private set; }

        private List<AffiliationModel> _affilInner;

        public Dictionary<string, Group> Groups { get; private set; }

        private List<Group> _groupInner;

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

        public void SetGroups(List<Group> groups)
        {
            _groupInner = groups;
            Groups = groups.ToDictionary(t => t.id);
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
    }

    public static class HolderExtensions
    {
        public static DataHolder Holder(this HttpContext httpContext)
        {
            return (DataHolder)httpContext.Items["Board"];
        }
    }
}
