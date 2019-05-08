using System;
using System.Threading.Tasks;

namespace Transfer
{
    class Program
    {
        static void Log(string what)
        {
            Console.WriteLine($"[{DateTime.Now}] " + what);
        }

        static async Task MainAsync(string[] args)
        {
            var domServer = new DomServer(args[0], args[1]);
            var fakeBoard = new FakeBoard(args[2], args[3]);
            var delayLength = int.Parse(args[4]);

            Log("Synchronizing contest information...");
            var contest = await domServer.GetContestAsync();
            await fakeBoard.PutAsync("contest", contest);

            Log("Synchronizing affiliations information...");
            var organizations = await domServer.GetOrganizationsAsync();
            await fakeBoard.PutAsync("organizations", organizations);

            Log("Synchronizing teams information...");
            var teams = await domServer.GetTeamsAsync();
            await fakeBoard.PutAsync("teams", teams);

            Log("Synchronizing problems information...");
            var problems = await domServer.GetProblemsAsync();
            await fakeBoard.PutAsync("problems", problems);

            while (true)
            {
                Log("Synchronizing scoreboard information...");
                var scoreboard = await domServer.GetScoreBoardAsync();
                await fakeBoard.PutAsync("scoreboard", scoreboard);
                await Task.Delay(delayLength);
            }
        }

        static void Main(string[] args)
        {
            if (args.Length != 5)
            {
                Console.WriteLine("./Transfer [domserver_url] [contest_id] [boarl_url] [auth_string] [delay_length_ms]");
                return;
            }

            MainAsync(args).GetAwaiter().GetResult();
        }
    }
}
