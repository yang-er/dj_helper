using System;

namespace Board.Models
{
#pragma warning disable IDE1006
    public class ScoreBoardModel
    {
        public string event_id { get; set; }
        public DateTime time { get; set; }
        public string contest_time { get; set; }
        public State state { get; set; }
        public Row[] rows { get; set; }
    }

    public class State
    {
        public DateTime? started { get; set; }
        public DateTime? ended { get; set; }
        public DateTime? frozen { get; set; }
        public DateTime? thawed { get; set; }
        public DateTime? finalized { get; set; }
        public DateTime? end_of_updates { get; set; }
    }

    public class Row
    {
        public int rank { get; set; }
        public string team_id { get; set; }
        public Score score { get; set; }
        public Problem[] problems { get; set; }
    }

    public class Score
    {
        public int num_solved { get; set; }
        public int total_time { get; set; }
    }

    public class Problem
    {
        public string label { get; set; }
        public string problem_id { get; set; }
        public int num_judged { get; set; }
        public int num_pending { get; set; }
        public bool solved { get; set; }
        public int time { get; set; }

        public string StyleClass(bool fb)
        {
            if (num_pending > 0)
                return "score_pending";
            else if (solved && fb)
                return "score_correct score_first";
            else if (solved && !fb)
                return "score_correct";
            else
                return "score_incorrect";
        }

        public string Tries()
        {
            string ans = "";
            if (num_pending > 0)
                ans += num_judged + " + " + num_pending;
            else if (num_judged > 0)
                ans += num_judged;
            if (num_pending + num_judged > 1)
                ans += " tries";
            else ans += " try";
            return ans;
        }
    }
#pragma warning restore
}
