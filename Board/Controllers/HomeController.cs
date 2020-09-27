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

        [Route("/{name}")]
        [Route("/")]
        public IActionResult Index(
            [FromRoute] string name,
            [FromQuery(Name = "affiliations[]")] string[] affiliations,
            [FromQuery(Name = "categories[]")] string[] categories,
            [FromQuery(Name = "clear")] string clear = "")
        {
            name ??= string.Empty;
            DataHolder holder;

            if (!DataService.Instance.Have(name))
            {
                Response.StatusCode = 404;
                ViewBag.Holder = holder = NotFoundItem.Value;
                return View("Menus");
            }
            else
            {
                ViewBag.Holder = holder = DataService.Instance[name];
            }

            if (holder.Contest.start_time > DateTime.Now)
                return View("Pending");

            ViewData["CurrentQuery"] = HttpContext.Request.QueryString.Value.Replace("&amp;", "&");
            var teamSource = holder.Teams;
            var source = holder.ScoreBoard.rows.Where(r => teamSource.ContainsKey(r.team_id)).Select(r => (r, teamSource[r.team_id]));

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

        static readonly Lazy<DataHolder> NotFoundItem = new Lazy<DataHolder>(() =>
        {
            var dh = new DataHolder("not-found", "Scoreboard", DataService.Instance._loggerFactory);
            
            dh.SetContest(new ContestModel
            {
                formal_name = "Scoreboard",
                penalty_time = 20,
                start_time = DateTime.UnixEpoch,
                end_time = DateTime.UnixEpoch.AddDays(1),
                duration = "24:00:00.000",
                scoreboard_freeze_duration = "0:00:00.000",
                id = "2",
                external_id = "demo",
                name = "Scoreboard",
                shortname = "demo",
            });

            return dh;
        });
    }
}
