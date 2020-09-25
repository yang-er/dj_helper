using Board.Models;
using Board.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;

namespace Board.Controllers
{
    [Route("api/{name}")]
    public class ApiController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            if (!context.RouteData.Values.TryGetValue("name", out var __name) ||
                !(__name is string name) ||
                !DataService.Instance.Have(name))
            {
                context.Result = NotFound();
                return;
            }

            HttpContext.Features.Set(DataService.Instance[name]);
        }

        [HttpGet]
        public ActionResult<string[]> Index()
        {
            return new string[] { "contest", "organizations", "problems", "scoreboard", "teams" };
        }

        [HttpPut("contest")]
        public IActionResult Contest([FromBody] ContestModel model)
        {
            HttpContext.Holder().SetContest(model);
            return Ok();
        }

        [HttpPut("organizations")]
        public IActionResult Organizations([FromBody] List<AffiliationModel> model)
        {
            HttpContext.Holder().SetOrganizations(model);
            return Ok();
        }

        [HttpPut("problems")]
        public IActionResult Problems([FromBody] List<ProblemModel> model)
        {
            HttpContext.Holder().SetProblems(model);
            return Ok();
        }

        [HttpPut("scoreboard")]
        public IActionResult Scoreboard([FromBody] ScoreBoardModel model)
        {
            HttpContext.Holder().SetScoreboard(model);
            return Ok();
        }

        [HttpPut("groups")]
        public IActionResult Groups([FromBody] List<Group> model)
        {
            foreach (var mod in model)
            {
                if (mod.color is null)
                {
                    mod.color = "#ffffff";
                }
            }

            HttpContext.Holder().SetGroups(model);
            return Ok();
        }

        [HttpDelete("scoreboard")]
        public IActionResult Scoreboard()
        {
            HttpContext.Holder().SetScoreboard(new ScoreBoardModel
            {
                rows = new Row[0],
                time = DateTime.Now,
                state = new State(),
                event_id = "-1",
                contest_time = "5:00:00.000"
            });

            return Ok();
        }

        [HttpPut("teams")]
        public IActionResult Teams([FromBody] List<TeamModel> model)
        {
            HttpContext.Holder().SetTeams(model);
            return Ok();
        }
    }
}
