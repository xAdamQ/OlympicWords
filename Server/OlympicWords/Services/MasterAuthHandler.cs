using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Shared;

namespace OlympicWords.Services
{
    public class MasterAuthSchemeOptions : AuthenticationSchemeOptions
    {
    }

    public class MasterAuthHandler : AuthenticationHandler<MasterAuthSchemeOptions>
    {
        public const string PROVIDER_NAME = "Master";

        private readonly SecurityManager securityManager;
        private readonly IScopeRepo scopeRepo;
        private readonly PersistantData persistantData;
        private readonly IOfflineRepo offlineRepo;
        private readonly ILogger<MasterAuthHandler> logger;

        public MasterAuthHandler(
            IOptionsMonitor<MasterAuthSchemeOptions> options,
            ILoggerFactory loggerFac,
            UrlEncoder encoder, ISystemClock clock, ILogger<MasterAuthHandler> logger,
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

        private const string
            NEGOTIATE_ENDPOINT = "/connect/negotiate",
            CONNECT_ENDPOINT = "/connect";

        //todo this is called each time, however I should have different handlers for different 
        //scopes, for example I need one to connect to room, and another for controllers.
        //For the upgrade part, it shouldn't happen implicitly when we login, it should happen after we 
        //login, this is more work, but a cleaner approach (DONE)
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            //there is an optimization FLAW that needs implementation: application tokens instead of
            //using provider tokens directly because this middleware is being hit several times and we can't 
            //depend on the slow process of authenticating the users from the external handlers and waiting for
            //the results, spamming them like that may get you banned also
            //THE SOLUTION: make app tokens with extended lifetime to authenticate locally after the first authentication
            //when the token expires, this means you have to check the provider and also update the user data from it
            //like the picture and name

            Request.Query.TryGetValue("provider", out var providerString);
            Request.Query.TryGetValue("access_token", out var accessToken);

            if (string.IsNullOrEmpty(accessToken))
                return AuthenticateResult.Fail("no access token provided");

            var providerParsed = Enum.TryParse(providerString, out ProviderType provider);
            if (!providerParsed)
                return AuthenticateResult.Fail($"specify a valid token provider, passed: {providerString}");

            //////extract inputs and validate them

            var profile = await securityManager.GetProfile(provider, accessToken);
            var user = await offlineRepo.GetUserAsync(accessToken, provider);

            if (user == null)
                user = await securityManager.SignUpAndInAsync(profile);
            else
                user = await securityManager.SignInAsync(profile, user);

            scopeRepo.SetRealOwner(user.Id);
            offlineRepo.SetCurrentUser(user);
            persistantData.FeedScope(scopeRepo);

            if (Request.Path == CONNECT_ENDPOINT &&
                (scopeRepo.IsUserPending() || scopeRepo.RoomUser?.Active == false))
                return AuthenticateResult.Fail("the user is playing in a room but tries to connect again");

            var genericClaims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                //this is the identifier used in the signalr, this claim type "NameIdentifier" could be changed with IUserIdProvider
            }; //this the only claims I can obtain from the payload

            var genericIdentity = new ClaimsIdentity
                (genericClaims, /*Scheme.Name*/ authenticationType: PROVIDER_NAME);
            var principal = new GenericPrincipal(genericIdentity, roles: null);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            scopeRepo.SetRealOwner(user.Id);

            logger.LogInformation("login succeeded for player: {userId} in {requestPath}", user.Id, Request.Path);
            return AuthenticateResult.Success(ticket);
        }
    }
}