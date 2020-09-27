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
        private readonly int _lop;

        public HdojFetcher(int cid, DataHolder data, string username, string userpass, int lop, ILogger logger)
        {
            _data = data;
            _cid = cid;
            _lop = lop;
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
                if (tt.StatusCode == HttpStatusCode.OK) return _loginStatus = true;
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
            if (idx == -1) { _loginStatus = false; throw new Exception("mark not found."); }
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
                if (itc.Length != _data.Problems.Count) throw new Exception("invalid data");
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

            var ctx = _data.Contest;
            scb.state = new State
            {
                started = ctx.start_time,
                ended = ctx.end_time < DateTime.Now ? ctx.end_time : default(DateTime?),
                thawed = ctx.end_time < DateTime.Now ? ctx.end_time : default(DateTime?),
                finalized = ctx.end_time < DateTime.Now ? ctx.end_time : default(DateTime?),
                frozen = ctx.end_time < DateTime.Now ? ctx.end_time : default(DateTime?),
            };

            if ((scb.rows.LastOrDefault().team_id == "print") == (_data.Contest.name == "jilin"))
                _data.SetScoreboard(scb);
        }

        public async Task Upd()
        {
            #region Problems
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
            #endregion

            #region Categories
            const string flag2 = "<tr><td width=5% bgcolor=";
            res = await _httpClient.GetStringAsync($"/contests/concern_person.php?cid={_cid}");
            const string flag3 = "...<td>";
            const string flag4 = "</td>";
            const string flag5 = "<td width= 15%";
            const string flag6 = "<td width= 39%";
            var aff = new Dictionary<string, AffiliationModel>();
            var org = new Dictionary<string, Group>();
            var tms = new List<TeamModel>();

            idx = res.IndexOf(flag2);
            while (idx != -1)
            {
                int idx2 = res.IndexOf(flag3, idx + flag2.Length);
                var row = res[idx..idx2];

                int stt = row.IndexOf(flag5) + flag5.Length;
                int sttt = row.IndexOf(flag4, stt);
                var teamId = row[stt..sttt];
                teamId = teamId.Substring(teamId.IndexOf('>') + 1);

                stt = row.IndexOf(flag6) + flag6.Length;
                sttt = row.IndexOf(flag4, stt);
                var teamName = row[stt..sttt];
                teamName = teamName.Substring(teamName.IndexOf('>') + 1);

                AffiliationModel GetAff(string affn)
                {
                    if (!aff.TryGetValue(affn, out var affff))
                    {
                        aff[affn] = affff = new AffiliationModel
                        {
                            icpc_id = affn,
                            shortname = affn,
                            formal_name = affn,
                            id = affn,
                            name = affn,
                        };
                    }

                    return affff;
                }

                Group GetGrp(string ss)
                {
                    if (!org.TryGetValue(ss, out var orggg))
                    {
                        org[ss] = orggg = new Group
                        {
                            icpc_id = ss,
                            sortorder = 0,
                            hidden = false,
                            color = ss.Contains("星") || ss.Contains("*") ? "#ffcc33"
                                  : ss.Contains("独立学院") ? "#33cc44"
                                  : ss.Contains("女") ? "#ff99cc"
                                  : "#ffffff",
                            id = ss,
                            name = ss,
                        };
                    }

                    return orggg;
                }

                void Parse(string id, string name)
                {
                    string catt, orgg, orgn;

                    if (_lop == 1)
                    {
                        name = name.Replace("<br>", " ").Replace("\n", " ");
                        var ss = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        name = ss[0];
                        orgg = orgn = GetAff(ss[2]).id;
                        catt = GetGrp(ss[1]).id;
                    }
                    else if (_lop == 2)
                    {
                        //team001 未来可期 白城师范学院 普通高校
                        name = name.Replace("<br>", " ").Replace("\n", " ");
                        var ss = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        name = string.Join(' ', ss[1..^2]);
                        orgg = orgn = GetAff(ss[^2]).id;
                        catt = GetGrp(ss[^1]).id;
                    }
                    else if (_lop == 3)
                    {
                        //team049 青阳 正式 哈尔滨工业大学
                        name = name.Replace("<br>", " ").Replace("\n", " ");
                        var ss = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        name = string.Join(' ', ss[1..^2]);
                        orgg = orgn = GetAff(ss[^1]).id;
                        catt = GetGrp(ss[^2]).id;
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }

                    tms.Add(new TeamModel
                    {
                        icpc_id = id,
                        externalid = id,
                        id = id,
                        members = "",
                        group_ids = new[] { catt },
                        affiliation = orgn,
                        organization_id = orgg,
                        name = name,
                    });
                }

                Parse(teamId, teamName);
                idx = res.IndexOf(flag2, idx2 + flag3.Length);
            }
            #endregion

            #region Retry Problems
            if (pmod.Count == 0)
            {
                var rklist = await _httpClient.GetStringAsync($"/contests/contest_ranklist.php?cid={_cid}&page=1");
                var idx1 = rklist.IndexOf("<td>1001</td>");
                var idx2 = rklist.IndexOf("</tr>", idx1);
                var lst = rklist[idx1..idx2].Trim().Split("</td><td>");
                if (lst[^1] != $"{1000 + lst.Length}</td>")
                    throw new InvalidOperationException();
                
                for (int i = 0; i < lst.Length; i++)
                {
                    pmod.Add(new ProblemModel
                    {
                        rgb = "#fff",
                        time_limit = 1.0,
                        test_data_count = 1,
                        ordinal = i + 1,
                        externalid = $"{i + 1001}",
                        name = "Unknown",
                        short_name = new string((char)('A' + i), 1),
                        color = "white",
                        label = "hduoj",
                        id = $"{i + 1001}",
                    });
                }
            }
            #endregion

            _data.SetProblems(pmod);
            _data.SetOrganizations(aff.Values.ToList());
            _data.SetGroups(org.Values.ToList());
            _data.SetTeams(tms);
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
