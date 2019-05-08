using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Transfer
{
    public class DomServer
    {
        readonly HttpClient httpClient;
        readonly string contestId;

        public DomServer(string url, string cid)
        {
            if (!url.Contains("http"))
                url = "http://" + url + "/domjudge";
            url += "/api/v4/";

            Console.WriteLine("DOMjudge api url: " + url);
            Console.WriteLine("DOMjudge contest id: " + cid);
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(url);
            contestId = cid;
        }

        public Task<string> GetContestAsync()
        {
            return httpClient.GetStringAsync("contests/" + contestId);
        }

        public Task<string> GetOrganizationsAsync()
        {
            return httpClient.GetStringAsync("contests/" + contestId + "/organizations");
        }

        public Task<string> GetProblemsAsync()
        {
            return httpClient.GetStringAsync("contests/" + contestId + "/problems");
        }

        public Task<string> GetTeamsAsync()
        {
            return httpClient.GetStringAsync("contests/" + contestId + "/teams");
        }

        public Task<string> GetScoreBoardAsync()
        {
            return httpClient.GetStringAsync("contests/" + contestId + "/scoreboard");
        }
    }
}
