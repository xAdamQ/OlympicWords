using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OlympicWords.Services;

namespace OlympicWords
{
    public class MasterAuthenticationSchemeOptions : AuthenticationSchemeOptions
    {
    }

    public class
        MasterAuthenticationHandler : AuthenticationHandler<MasterAuthenticationSchemeOptions>
    {
        public const string ProviderName = "Master";

        private ILogger<MasterAuthenticationHandler> logger;
        private readonly SecurityManager securityManager;

        public MasterAuthenticationHandler(
            IOptionsMonitor<MasterAuthenticationSchemeOptions> options,
            ILoggerFactory loggerFac,
            UrlEncoder encoder, ISystemClock clock, ILogger<MasterAuthenticationHandler> logger,
            SecurityManager securityManager)
            : base(options, loggerFac, encoder, clock)
        {
            this.logger = logger;
            this.securityManager = securityManager;
        }

        /// <summary>
        /// gets string after 6 chars (bearer)
        /// exceptions: token header doesn't exist - does't have 6 chars (for bearer {so it can be any word with 6 char!})
        /// exceptions caught on higher level, you can identify them to avoid your faults
        /// look at the disabled function
        /// </summary>
        private string GetAuthorizationHeader()
        {
            string authorizationHeader = Request.Headers["Authorization"];
            return authorizationHeader.Substring("bearer".Length).Trim();
        }

        public const string PROVIDER_NAME = "Master";

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            logger.LogInformation("############################ some one is logging in");
            Request.Query.TryGetValue("demo", out var demo);
            Request.Query.TryGetValue("name", out var name);
            Request.Query.TryGetValue("access_token", out var accessToken);

            var user = await securityManager.SignInAsync(accessToken,
                (int) ExternalIdType.Demo, name, accessToken);
            //these data maybe fetched from the auth provider in a custom way

            var genericClaims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
            }; //this the only claims I can
            var genericIdentity =
                new ClaimsIdentity(genericClaims, /*Scheme.Name*/ PROVIDER_NAME);
            //fbig shoud (in theory) have more than idnetity, but the auth provider is the same.. how to differentiat

            var principal = new GenericPrincipal(genericIdentity, null);

            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            // _logger.LogInformation($"login succeeded for player: {user.Id}");
            return AuthenticateResult.Success(ticket);
        }
    }
}