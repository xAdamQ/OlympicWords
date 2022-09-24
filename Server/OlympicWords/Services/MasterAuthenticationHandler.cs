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
using OlympicWords.Services.Exceptions;
using OlympicWords.Services.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OlympicWords.Services
{
    public class MasterAuthenticationSchemeOptions : AuthenticationSchemeOptions
    {
    }

    public class
        MasterAuthenticationHandler : AuthenticationHandler<MasterAuthenticationSchemeOptions>
    {
        public const string ProviderName = "Master";

        private readonly SecurityManager securityManager;
        private readonly IScopeRepo scopeRepo;
        private ILogger<MasterAuthenticationHandler> logger;

        public MasterAuthenticationHandler(
            IOptionsMonitor<MasterAuthenticationSchemeOptions> options,
            ILoggerFactory loggerFac,
            UrlEncoder encoder, ISystemClock clock, ILogger<MasterAuthenticationHandler> logger,
            SecurityManager securityManager, IScopeRepo scopeRepo)
            : base(options, loggerFac, encoder, clock)
        {
            this.securityManager = securityManager;
            this.scopeRepo = scopeRepo;
            this.logger = logger;
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

        /*old authorization header function

                    #region validate token header structure
                    if (!Request.Headers.ContainsKey("Authorization"))
                    {
                        return AuthenticateResult.Fail("Unauthorized");
                    }

                    string authorizationHeader = Request.Headers["Authorization"];
                    if (string.IsNullOrEmpty(authorizationHeader))
                    {
                        return AuthenticateResult.NoResult();
                    }

                    if (!authorizationHeader.StartsWith("bearer", StringComparison.OrdinalIgnoreCase))
                    {
                        return AuthenticateResult.Fail("Unauthorized");
                    }

                    string token = authorizationHeader.Substring("bearer".Length).Trim();

                    if (string.IsNullOrEmpty(token))
                    {
                        return AuthenticateResult.Fail("Unauthorized");
                    }
                    #endregion

        */

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            logger.LogInformation("some one is logging in");

            try
            {
                Request.Query.TryGetValue("access_token", out var figToken);
                Request.Query.TryGetValue("huaweiAuthCode", out var huaweiAuthCode);
                Request.Query.TryGetValue("fb_access_token", out var fbAccToken);


                Request.Query.TryGetValue("name", out var name);
                Request.Query.TryGetValue("pictureUrl", out var pictureUrl);

                Request.Query.TryGetValue("demo", out var demo);
                //can have exceptions, they are caught here

                //todo unify access token parameter with int type if you have time for huawei submission

                User user;
                if (String.IsNullOrEmpty(demo)) //not demo
                {
                    if (!string.IsNullOrEmpty(huaweiAuthCode))
                    {
                        logger.LogInformation("hauwei login with token: " + huaweiAuthCode);

                        try
                        {
                            var token = await GetTokenByHuaweiAuthCode(huaweiAuthCode);

                            var userData = await GetUserDatByToken(token);

                            user = await securityManager.SignInAsync(userData.openId,
                                (int) ExternalIdType.Huawei, userData.name, userData.picUrl);
                        }
                        catch (HuaweiApiFailure exc)
                        {
                            return AuthenticateResult.Fail(exc.Message);
                        }
                    }
                    else if (!string.IsNullOrEmpty(figToken))
                    {
                        if (!securityManager.ValidateToken(figToken, out var playerId))
                            return AuthenticateResult.Fail("fig token validation failed");

                        user = await securityManager.SignInAsync(playerId,
                            (int) ExternalIdType.Fbig, name, pictureUrl);
                    }
                    else if (!string.IsNullOrEmpty(fbAccToken))
                    {
                        try
                        {
                            var isValid = await securityManager.ValidateFbAccToken(fbAccToken);

                            if (!isValid)
                                return AuthenticateResult.Fail(
                                    "the given fb acc token is not valid");

                            var userData = await securityManager.GetFbProfile(fbAccToken);

                            user = await securityManager.SignInAsync(userData.id,
                                (int) ExternalIdType.Facebook, userData.name, userData.picUrl);
                        }
                        //you should send bad input exc only to him not fb api error also
                        catch (Exception e)
                        {
                            return AuthenticateResult.Fail(e.Message);
                        }
                    }
                    else
                    {
                        return AuthenticateResult.Fail("no access token provided for signing in");
                    }
                }
                else //demo
                {
                    user = await securityManager.SignInAsync(figToken,
                        (int) ExternalIdType.Demo, name, pictureUrl);
                }

                if (scopeRepo.IsUserActive(user.Id) &&
                    scopeRepo.GetActiveUser(user.Id).IsDisconnected == false)
                    return AuthenticateResult.Fail(
                        "the user is connected and trying to connect again");

                var genericClaims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.Id),

                    //this is the identifier used in the signalr, this claim type "NameIdentifier" could be changed with IUserIdProvider
                    //new Claim(ClaimTypes.Name, user.UserName),
                    //new Claim(ClaimTypes.Email, user.Email),
                    // new Claim("UserType", "General"),//role?
                }; //this the only claims I can obtain from the payload

                var genericIdentity =
                    new ClaimsIdentity(genericClaims, /*Scheme.Name*/ ProviderName);
                //fbig shoud (in theory) have more than idnetity, but the auth provider is the same.. how to differentiat

                var principal = new GenericPrincipal(genericIdentity, null);

                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                // _logger.LogInformation($"login succeeded for player: {user.Id}");
                return AuthenticateResult.Success(ticket);
            }
            catch (Exception exception)
            {
                return AuthenticateResult.Fail("auth exception with message: " + exception.Message);
            } //todo: are you sure it's a bad request not internal server error?, you should use specific expected errors for user fault
        }

        public static async Task<string> GetTokenByHuaweiAuthCode(string code)
        {
            var data = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                {"grant_type", "authorization_code"},
                {"client_id", "104645983"},
                {
                    "client_secret",
                    "70fa010a05f4a3c9fc389d0046cd69cacc7b50f8d26b09e65940c9ef37abf416"
                },
                {"code", code},
                {"redirect_uri", "hms://redirect_uri"}
            });

            var url = "https://oauth-login.cloud.huawei.com/oauth2/v3/token";
            using var client = new HttpClient();

            var response = await client.PostAsync(url, data);

            var result = response.Content.ReadAsStringAsync().Result;

            dynamic obj = JObject.Parse(result);

            if (obj.access_token == null)
                throw new HuaweiApiFailure(
                    "couldn't huawei token by the given code, with response: " + result);

            return obj.access_token;
        }

        public static async Task<(string name, string picUrl, string openId)> GetUserDatByToken(
            string token)
        {
            var data = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                {"access_token", token},
                {"getNickName", "1"},
            });

            var url = "https://account.cloud.huawei.com/rest.php?nsp_svc=GOpen.User.getInfo";
            using var client = new HttpClient();

            var response = await client.PostAsync(url, data);

            var result = response.Content.ReadAsStringAsync().Result;

            dynamic obj = JObject.Parse(result);

            if (obj.openID == null)
                throw new HuaweiApiFailure("openID found null, with response: " + result);

            return (obj.displayName, obj.headPictureURL, obj.openID);
        }


        [Serializable]
        public class HuaweiApiFailure : Exception
        {
            public HuaweiApiFailure(string message) : base(message)
            {
            }
        }
    }
}