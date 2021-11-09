using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pomelo.Wow.EventRegistration.Web.Models;

namespace Pomelo.Wow.EventRegistration.Authentication
{
    [ExcludeFromCodeCoverage]
    public class TokenAuthenticateHandler : AuthenticationHandler<TokenOptions>
    {
        public new const string Scheme = "Token";
        private static ConcurrentDictionary<string, User> SessionDic = new ConcurrentDictionary<string, User>();

        private WowContext _db;

        public TokenAuthenticateHandler(
            IOptionsMonitor<TokenOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            WowContext db)
            : base(options, logger, encoder, clock)
        {
            this._db = db;
        }

        public static string GenerateToken(User user)
        {
            var token = Guid.NewGuid().ToString().Replace("-", "");
            SessionDic.TryAdd(token, user);
            return token;
        }

        public static bool Check(string token)
        { 
            return SessionDic.ContainsKey(token);
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authorization = Request.Headers["Authorization"].ToArray();
            if (authorization.Length == 0)
            {
                if (!string.IsNullOrEmpty(Request.Query["token"]))
                {
                    authorization = new[] { $"Token {Request.Query["token"]}" };
                }
                else if (!string.IsNullOrEmpty(Request.Query["session"]))
                {
                    authorization = new[] { $"Session {Request.Query["token"]}" };
                }
                else
                {
                    return AuthenticateResult.NoResult();
                }
            }

            User tokenOwner = null;
            if (authorization.First().StartsWith("Token", StringComparison.OrdinalIgnoreCase))
            {
                var t = authorization.First().Substring("Token ".Length);

                if (!SessionDic.TryGetValue(t, out var user))
                {
                    return AuthenticateResult.NoResult();
                }
                tokenOwner = user;
            }
            else
            {
                return AuthenticateResult.NoResult();
            }

            var claimIdentity = new ClaimsIdentity(Scheme, ClaimTypes.Name, ClaimTypes.Role);
            claimIdentity.AddClaim(new Claim(ClaimTypes.Name, tokenOwner.Id.ToString()));
            claimIdentity.AddClaim(new Claim(ClaimTypes.Role, tokenOwner.Role.ToString()));
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(claimIdentity), Scheme);
            await _db.SaveChangesAsync();

            return AuthenticateResult.Success(ticket);
        }
    }
}
