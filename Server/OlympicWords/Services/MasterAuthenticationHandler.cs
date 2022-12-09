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
        private readonly IOfflineRepo offlineRepo;
        private readonly ILogger<MasterAuthenticationHandler> logger;

        public MasterAuthenticationHandler(
            IOptionsMonitor<MasterAuthenticationSchemeOptions> options,
            ILoggerFactory loggerFac,
            UrlEncoder encoder, ISystemClock clock, ILogger<MasterAuthenticationHandler> logger,
            SecurityManager securityManager, IScopeRepo scopeRepo, PersistantData persistantData,
            IOfflineRepo offlineRepo)
            : base(options, loggerFac, encoder, clock)
        {
            this.securityManager = securityManager;
            this.scopeRepo = scopeRepo;
            this.persistantData = persistantData;
            this.offlineRepo = offlineRepo;
            this.logger = logger;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            //can have exceptions, they are caught here
            try
            {
                Request.Query.TryGetValue("provider", out var providerString);
                Request.Query.TryGetValue("access_token", out var accessToken);

                //exists only in the upgrade request
                Request.Query.TryGetValue("original_provider", out var originalProviderString);
                Request.Query.TryGetValue("original_provider_token", out var originalProviderToken);
                Request.Query.TryGetValue("link_overwrite", out var linkOverwrite);

                var originalProviderParsed = Enum.TryParse(originalProviderString, out ExternalIdType originalProvider);

                User user;
                var providerParsed = Enum.TryParse(providerString, out ExternalIdType provider);

                if (string.IsNullOrEmpty(accessToken))
                    return AuthenticateResult.Fail("no access token provided");
                if (!providerParsed)
                    return AuthenticateResult.Fail($"specify a valid token provider, passed: {providerString}");

                try
                {
                    var profile = await securityManager.GetProfile(provider, accessToken);
                    user = await securityManager.GetDiskUser(profile);

                    if (originalProviderParsed)
                    {
                        if (string.IsNullOrEmpty(originalProviderToken))
                        {
                            logger.LogError("an original provider name exits, but no token");
                            //I won't break here, it is not terminal error, it should be a user fault only, but just in case
                        }
                        else
                        {
                            var originalProfile = new ProviderUser
                            {
                                Id = originalProviderToken,
                                Provider = originalProvider,
                            };
                            var originalUser = await securityManager.GetDiskUser(originalProfile);

                            if (user == null && originalUser != null)
                            {
                                await securityManager.LinkUser(originalUser.Id, profile);
                                user = originalUser;
                            }
                            else if (originalUser == null)
                            {
                                throw new BadUserInputException("you're trying to link to a non-existing user");
                            }
                            else if (linkOverwrite == "true") //both user and originalUser are not null
                            {
                                await offlineRepo.DeleteUserAsync(originalUser.Id);
                                await securityManager.LinkUser(user.Id, originalProfile);
                            }
                            else
                            {
                                throw new BadUserInputException
                                    ("overwriting is false and you're trying rewrite an existing user");
                            }
                        }
                    }
                    //in case we upgrade from guest to facebook

                    if (user == null)
                        user = await securityManager.SignUpAndInAsync(profile);
                    else
                        user = await securityManager.SignInAsync(profile, user);
                }

                catch (Exception e)
                {
                    return AuthenticateResult.Fail(e.Message);
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

                logger.LogInformation("login succeeded for player: {UserId}", user.Id);
                return AuthenticateResult.Success(ticket);
            }
            catch (Exception exception)
            {
                return AuthenticateResult.Fail("auth exception with message: " + exception.Message);
            } //todo: are you sure it's a bad request not internal server error?, you should use specific expected errors for user fault
        }
    }
}