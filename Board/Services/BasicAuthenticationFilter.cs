using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Text;

namespace Board.Services
{
    /// <summary>
    /// 通用 Basic Authorization 过滤器
    /// </summary>
    public class BasicAuthenticationFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.Request.Path.Value.Contains("api")) return;
            var auth = ParseHeader(context.HttpContext.Request);

            if (auth is null)
            {
                context.Result = new NeedAuthenticationResult();
            }
            else if (!CheckAuthorize(context.HttpContext.RequestServices, auth))
            {
                context.Result = new StatusCodeResult(403);
            }
        }

        protected virtual Tuple<string, string> ParseHeader(HttpRequest request)
        {
            request.Headers.TryGetValue("Authorization", out var auth);
            if (auth.Count != 1) return null;
            var authString = auth.First();
            if (!authString.StartsWith("Basic ")) return null;
            var authHeader = UnBase64(authString.Substring(6));
            var tokens = authHeader.Split(':', 2);
            if (tokens.Length < 2) return null;
            return new Tuple<string, string>(tokens[0], tokens[1]);
        }

        /// <summary>
        /// 解开Base64编码。
        /// </summary>
        /// <param name="value">Base64字符串。</param>
        /// <returns>原字符串。</returns>
        public static string UnBase64(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            var bytes = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(bytes);
        }

        protected virtual bool CheckAuthorize(IServiceProvider httpContext, Tuple<string, string> auth)
        {
            auth = auth ?? throw new ArgumentNullException(nameof(auth));
            return httpContext.GetRequiredService<AuthorizationService>().Authorize(auth);
        }

        private class NeedAuthenticationResult : ActionResult
        {
            const string HeaderName = "WWW-Authenticate";
            const string HeaderValue = "Basic realm=\"Judge REST\"";

            public override void ExecuteResult(ActionContext context)
            {
                base.ExecuteResult(context);
                context.HttpContext.Response.Headers.Add(HeaderName, HeaderValue);
                context.HttpContext.Response.StatusCode = 401;
            }
        }
    }
}
