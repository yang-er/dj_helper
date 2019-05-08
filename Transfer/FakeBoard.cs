using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Transfer
{
    public class FakeBoard
    {
        readonly HttpClient httpClient;

        public FakeBoard(string url, string auth)
        {
            if (!url.Contains("http"))
                url = "http://" + url + "";
            url += "/api/";

            Console.WriteLine("FakeBoard api url: " + url);
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(url);

            var authByte = Encoding.Default.GetBytes(auth);
            var authBase64 = Convert.ToBase64String(authByte);
            var headerVal = new AuthenticationHeaderValue("Basic", authBase64);
            httpClient.DefaultRequestHeaders.Authorization = headerVal;
        }

        public Task PutAsync(string url, string content)
        {
            return httpClient.PutAsync(url, new StringContent(content, Encoding.UTF8, "text/json"));
        }
    }
}
