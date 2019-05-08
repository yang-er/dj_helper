using Board.Models;
using Board.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace Board.Controllers
{
    public class ApiController : Controller
    {
        [Route("/api/")]
        [HttpGet]
        public ActionResult<string[]> Index()
        {
            return new string[] { "contest", "organizations", "problems", "scoreboard", "teams" };
        }

        [Route("/api/contest")]
        [HttpPut]
        public IActionResult Contest([FromBody] ContestModel model)
        {
            DataService.Instance.SetContest(model);
            return Ok();
        }

        [Route("/api/organizations")]
        [HttpPut]
        public IActionResult Organizations([FromBody] List<AffiliationModel> model)
        {
            DataService.Instance.SetOrganizations(model);
            return Ok();
        }

        [Route("/api/problems")]
        [HttpPut]
        public IActionResult Problems([FromBody] List<ProblemModel> model)
        {
            DataService.Instance.SetProblems(model);
            return Ok();
        }

        [Route("/api/scoreboard")]
        [HttpPut]
        public IActionResult Scoreboard([FromBody] ScoreBoardModel model)
        {
            DataService.Instance.SetScoreboard(model);
            return Ok();
        }

        [Route("/api/scoreboard")]
        [HttpDelete]
        public IActionResult Scoreboard()
        {
            DataService.Instance.SetScoreboard(new ScoreBoardModel
            {
                rows = new Row[0],
                time = DateTime.Now,
                state = new State(),
                event_id = "-1",
                contest_time = "5:00:00.000"
            });

            return Ok();
        }

        [Route("/api/teams")]
        [HttpPut]
        public IActionResult Teams([FromBody] List<TeamModel> model)
        {
            DataService.Instance.SetTeams(model);
            return Ok();
        }
    }
}
