using System;

namespace Board.Models
{
#pragma warning disable IDE1006
    public class ContestModel
    {
        public string formal_name { get; set; }
        public int penalty_time { get; set; }
        public DateTime start_time { get; set; }
        public DateTime end_time { get; set; }
        public string duration { get; set; }
        public string scoreboard_freeze_duration { get; set; }
        public string id { get; set; }
        public string external_id { get; set; }
        public string name { get; set; }
        public string shortname { get; set; }
    }
#pragma warning restore
}
