using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Board.Services
{
    public class AuthorizationService
    {
        private readonly Dictionary<string, string> dict;

        public AuthorizationService()
        {
            dict = new Dictionary<string, string>
            {
                { "username", "password" }
            };
        }

        public bool Authorize(Tuple<string, string> auth)
        {
            return dict.ContainsKey(auth.Item1)
                && dict[auth.Item1] == auth.Item2;
        }
    }
}
