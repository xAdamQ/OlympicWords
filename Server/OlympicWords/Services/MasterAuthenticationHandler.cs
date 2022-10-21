using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using OlympicWords.Data;

namespace OlympicWords.Services
{
    public class MasterAuthenticationSchemeOptions : AuthenticationSchemeOptions
    {
    }

    public class
        MasterAuthenticationHandler : AuthenticationHandler<MasterAuthenticationSchemeOptions>
    {
        public const string PROVIDER_NAME = "Master";

        private readonly SecurityManager securityManager;
        private readonly IScopeRepo scopeRepo;
        private readonly PersistantData persistantData;
        private readonly ILogger<MasterAuthenticationHandler> logger;

        public MasterAuthenticationHandler(
            IOptionsMonitor<MasterAuthenticationSchemeOptions> options,
            ILoggerFactory loggerFac,
            UrlEncoder encoder, ISystemClock clock, ILogger<MasterAuthenticationHandler> logger,
            SecurityManager securityManager, IScopeRepo scopeRepo, PersistantData persistantData)
            : base(options, loggerFac, encoder, clock)
        {
            this.securityManager = securityManager;
            this.scopeRepo = scopeRepo;
            this.persistantData = persistantData;
            this.logger = logger;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            //can have exceptions, they are caught here
            try
            {
                Request.Query.TryGetValue("provider", out var provider);
                Request.Query.TryGetValue("access_token", out var accessToken);

                User user;
                if (string.IsNullOrEmpty(accessToken))
                    return AuthenticateResult.Fail("no access token provided");
                if (string.IsNullOrEmpty(provider))
                    return AuthenticateResult.Fail("specify the token provider");

                if (provider == "demo")
                {
                    var demoProvider = new ProviderUser
                    {
                        Id = accessToken,
                        Name = new Guid().ToString()[..8],
                        Provider = ExternalIdType.Demo,
                    };
                    user = await securityManager.SignInAsync(demoProvider);
                }
                else if (provider == "huawei")
                {
                    logger.LogInformation("huawei login with token: {token}", accessToken);

                    try
                    {
                        var token = await SecurityManager.GetTokenByHuaweiAuthCode(accessToken);

                        var userData = await SecurityManager.GetHuaweiUserDataByToken(token);

                        user = await securityManager.SignInAsync(userData);
                    }
                    catch (SecurityManager.HuaweiApiFailure exc)
                    {
                        return AuthenticateResult.Fail(exc.Message);
                    }
                }
                else if (provider == "facebook")
                {
                    try
                    {
                        var isValid = await securityManager.ValidateFbAccToken(accessToken);

                        if (!isValid)
                            return AuthenticateResult
                                .Fail("the given facebook token is not valid");

                        var fbProfile = await SecurityManager.GetFbProfile(accessToken);

                        user = await securityManager.SignInAsync(fbProfile);
                        
                        
                    }
                    //you should send bad input exc only to him not fb api error also
                    catch (Exception e)
                    {
                        return AuthenticateResult.Fail(e.Message);
                    }
                }
                else
                {
                    return AuthenticateResult.Fail("the passed provider is not supported");
                }

                persistantData.FeedScope(scopeRepo);

                if (Request.Path == "/connect" &&
                    scopeRepo.IsUserActive(user.Id) &&
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
                    new ClaimsIdentity(genericClaims, /*Scheme.Name*/ PROVIDER_NAME);

                var principal = new GenericPrincipal(genericIdentity, null);

                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                logger.LogInformation($"login succeeded for player: {user.Id}");
                return AuthenticateResult.Success(ticket);
            }
            catch (Exception exception)
            {
                return AuthenticateResult.Fail("auth exception with message: " + exception.Message);
            } //todo: are you sure it's a bad request not internal server error?, you should use specific expected errors for user fault
        }
    }
}