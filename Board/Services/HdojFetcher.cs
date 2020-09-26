using Board.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Board.Services
{
    public class HdojFetcher
    {
        private readonly DataHolder _data;
        private readonly int _cid;
        private readonly HttpClient _httpClient;
        private readonly HttpContent _loginContent;
        private readonly ILogger _logger;
        private bool _loginStatus;

        public HdojFetcher(int cid, DataHolder data, string username, string userpass, ILogger logger)
        {
            _data = data;
            _cid = cid;
            _logger = logger;

            var handler = new HttpClientHandler();
            handler.UseCookies = true;
            handler.CookieContainer = new CookieContainer();
            handler.AllowAutoRedirect = false;

            _httpClient = new HttpClient(handler);
            _httpClient.BaseAddress = new Uri("http://acm.hdu.edu.cn/");

            _loginContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["login"] = "Sign In",
                ["username"] = username,
                ["userpass"] = userpass,
            });
        }

        private async Task<bool> Login()
        {
            try
            {
                using var tt = await _httpClient.GetAsync($"/contests/contest_show.php?cid={_cid}");
                if (tt.StatusCode == HttpStatusCode.OK) return true;
                var loginMessage = new HttpRequestMessage(HttpMethod.Post, $"userloginex.php?action=login&cid={_cid}&notice=0");
                loginMessage.Content = _loginContent;
                loginMessage.Headers.Referrer = new Uri($"http://acm.hdu.edu.cn/userloginex.php?cid={_cid}");

                using var result = await _httpClient.SendAsync(loginMessage);
                if (result.StatusCode == System.Net.HttpStatusCode.Found &&
                    result.Headers.TryGetValues("Location", out var values) &&
                    values.Single() == $"/contests/contest_show.php?cid={_cid}")
                {
                    _logger.LogInformation("Login succeeded.");
                    return _loginStatus = true;
                }
                else
                {
                    _logger.LogInformation("Unknown response {code}.", result.StatusCode);
                    return _loginStatus = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "failed to login");
                return _loginStatus = false;
            }
        }

        private async Task<string> Try()
        {
            try
            {
                using var content = await _httpClient.GetAsync($"/contests/client_ranklist.php?cid={_cid}");
                if (content.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    _logger.LogError("Unknown status code {code}.", content.StatusCode);
                    return null;
                }
                return await content.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "failed to fetch");
                return null;
            }
        }

        private void Parse(string content)
        {
            const string mark = "</tr><script language=javascript>";
            var idx = content.LastIndexOf(mark);
            Debug.Assert(idx != -1);
            var lastRnk = new Score { num_solved = 999 };
            int rk = 1, rk2 = 1;

            int FindEnd(int idxLeft) => content.IndexOf("\",1);", idxLeft) + 5;

            (Score, Problem[]) GetSubstain(string content)
            {
                int left = 0, right = content.Length - 1;
                for (int leftCount = 3; leftCount > 0; right--)
                    if (content[right] == '"')
                        leftCount--;
                for (left = right; content[left] != '"'; left--) ;
                left++;
                var scb = content.Substring(left, right - left + 1);
                var itc = scb.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Debug.Assert(itc.Length == _data.Problems.Count);
                var result = new Problem[itc.Length];
                var res = new Score();

                for (int i = 0; i < itc.Length; i++)
                {
                    result[i] = new Problem
                    {
                        problem_id = $"{i + 1001}",
                        label = new string((char)('A' + i), 1),
                    };

                    if (itc[i] == "@") continue;
                    
                    if (itc[i][0] == '(')
                    {
                        result[i].num_judged = int.Parse(itc[i][2..^1]);
                    }
                    else if (itc[i].Length == 8)
                    {
                        var minss = itc[i].Split(':');
                        result[i].num_judged = 1;
                        result[i].solved = true;
                        res.total_time += result[i].time = int.Parse(minss[0]) * 60 + int.Parse(minss[1]);
                        res.num_solved++;
                    }
                    else
                    {
                        var minss = itc[i].Substring(0, 8).Split(':');
                        result[i].num_judged = 1 + int.Parse(itc[i][14..^1]);
                        result[i].solved = true;
                        res.total_time += result[i].time = int.Parse(minss[0]) * 60 + int.Parse(minss[1]);
                        res.total_time += (result[i].num_judged - 1) * 20;
                        res.num_solved++;
                    }
                }

                return (res, result);
            }

            var scb = new ScoreBoardModel();
            scb.rows = new List<Row>();
            var inx = content.IndexOf("pr(", idx + mark.Length);
            while (inx != -1)
            {
                int right = FindEnd(inx + 2);
                var row = new Row();
                var ctd = content[inx..right];
                (row.score, row.problems) = GetSubstain(ctd);

                int idt = ctd.IndexOf($"\",{row.score.num_solved},\"");
                int lft = idt-1; for (; ctd[lft] != '"'; lft--) ; lft++;
                row.team_id = ctd[lft..idt];
                
                if (row.score.num_solved < lastRnk.num_solved || row.score.total_time > lastRnk.total_time)
                    (rk2, lastRnk) = (rk, row.score);
                row.rank = rk2;

                inx = content.IndexOf("pr(", right);
                rk++;
                scb.rows.Add(row);
            }

            _data.SetScoreboard(scb);
        }

        public async Task Upd()
        {
            var res = await _httpClient.GetStringAsync($"/contests/contest_show.php?cid={_cid}");

            var pmod = new List<ProblemModel>();

            int pid = 1001;
            const string flag = "<a href=\"contest_showproblem.php?pid=";
            int idx = res.IndexOf(flag);

            while (idx != -1)
            {
                int idx2 = res.IndexOf('>', idx + flag.Length);
                int idx3 = res.IndexOf("</a>", idx2);

                pmod.Add(new ProblemModel
                {
                    rgb = "#fff",
                    time_limit = 1.0,
                    test_data_count = 1,
                    ordinal = pid - 1000,
                    externalid = $"{pid}",
                    name = res.Substring(idx2 + 1, idx3 - idx2 - 1),
                    short_name = new string((char)('A' + pid - 1001), 1),
                    color = "white",
                    label = "hduoj",
                    id = $"{pid}",
                });

                pid++;
                idx = res.IndexOf(flag, idx3);
            }

            _data.SetProblems(pmod);
        }

        public async Task Work()
        {
            if (!_loginStatus)
            {
                if (await Login())
                {
                    await Upd();
                }
            }

            var content = await Try();

            if (content == null)
            {
                if (await Login())
                {
                    await Upd();
                    content = await Try();
                }
            }

            if (content != null)
                Parse(content);
        }
    }
}
