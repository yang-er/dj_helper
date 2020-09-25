using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Board.Models;
using Board.Services;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Board.Controllers
{
    public class HomeController : Controller
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);

            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                if (context.Result is ViewResult viewResult)
                {
                    context.Result = new PartialViewResult
                    {
                        TempData = viewResult.TempData,
                        ViewData = viewResult.ViewData,
                        ViewEngine = viewResult.ViewEngine,
                        ViewName = viewResult.ViewName,
                        StatusCode = 200,
                        ContentType = "text/html",
                    };
                }
            }
        }

        [Route("/")]
        public IActionResult Index(
            [FromQuery(Name = "affiliations[]")] string[] affiliations,
            [FromQuery(Name = "categories[]")] string[] categories,
            [FromQuery(Name = "clear")] string clear = "")
        {
            if (DataService.Instance.Contest.start_time > DateTime.Now)
                return View("Pending");

            ViewData["CurrentQuery"] = HttpContext.Request.QueryString.Value.Replace("&amp;", "&");
            var teamSource = DataService.Instance.Teams;
            var source = DataService.Instance.ScoreBoard.rows.Select(r => 
                (r, teamSource.ContainsKey(r.team_id) ? teamSource[r.team_id] : new TeamModel
                {
                    group_ids = new[] { "4" },
                    affiliation = "Unknown Affiliation",
                    externalid = "n/a",
                    icpc_id = "n/a",
                    id = r.team_id,
                    members = "",
                    name = "Unknown Team",
                    organization_id = "n/a"
                }));

            if (clear == "clear")
            {
                affiliations = new string[0];
                categories = new string[0];
            }

            if (affiliations.Length > 0)
            {
                var aff = new HashSet<string>(affiliations);
                source = source.Where(t => aff.Contains(t.Item2.organization_id));
                ViewData["Filter_affiliations"] = aff;
            }

            if (categories.Length > 0)
            {
                var cat = new HashSet<string>(categories);
                source = source.Where(t => cat.Contains(t.Item2.group_ids[0]));
                ViewData["Filter_categories"] = cat;
            }

            return View(source.Select(t => new RankTermModel
            {
                Base = t.r,
                Team = t.Item2
            }).ToList());
        }
    }
}
