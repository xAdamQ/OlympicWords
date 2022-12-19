using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using OlympicWords.Data;
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

        #region old full
        // //todo this is called each time, however I should have different handlers for different 
        // //todo scopes, for example I need one to connect to room, another for controllers
        // //todo and for the upgrade part, it shouldn't happen implicitly when we login, it should happen after we 
        // //todo login, this is more work, but a cleaner approach
        // protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        // {
        //     //can have exceptions, they are caught here
        //     try
        //     {
        //         Request.Query.TryGetValue("provider", out var providerString);
        //         Request.Query.TryGetValue("access_token", out var accessToken);
        //
        //         if (string.IsNullOrEmpty(accessToken))
        //             return AuthenticateResult.Fail("no access token provided");
        //
        //         //exists only in the upgrade request
        //         Request.Query.TryGetValue("original_provider", out var originalProviderString);
        //         Request.Query.TryGetValue("original_provider_token", out var originalProviderToken);
        //         Request.Query.TryGetValue("link_overwrite", out var linkOverwrite);
        //
        //         var originalProviderParsed = Enum.TryParse(originalProviderString, out ProviderType originalProvider);
        //         var providerParsed = Enum.TryParse(providerString, out ProviderType provider);
        //
        //         if (!providerParsed)
        //             return AuthenticateResult.Fail($"specify a valid token provider, passed: {providerString}");
        //
        //         Request.Query.TryGetValue("bet_choice", out var betChoice);
        //         Request.Query.TryGetValue("capacity_choice", out var capacityChoice);
        //
        //         if (!int.TryParse(betChoice, out _) || !int.TryParse(capacityChoice, out _))
        //             return AuthenticateResult.Fail("Invalid bet or capacity choice");
        //
        //         //////extract inputs and validate them
        //
        //         var profile = await securityManager.GetProfile(provider, accessToken);
        //         var user = await securityManager.GetDiskUser(profile);
        //
        //         if (originalProviderParsed)
        //         {
        //             if (string.IsNullOrEmpty(originalProviderToken))
        //             {
        //                 logger.LogError("an original provider name exits, but no token");
        //                 //I won't break here, it is not terminal error, it should be a user fault only, but just in case
        //             }
        //             else
        //             {
        //                 var originalProfile = new ProviderUser
        //                 {
        //                     Id = originalProviderToken,
        //                     Provider = originalProvider,
        //                 };
        //                 var originalUser = await securityManager.GetDiskUser(originalProfile);
        //
        //                 if (user == null && originalUser != null)
        //                 {
        //                     await securityManager.LinkUser(originalUser.Id, profile);
        //                     user = originalUser;
        //                 }
        //                 else if (originalUser == null)
        //                 {
        //                     throw new BadUserInputException("you're trying to link to a non-existing user");
        //                 }
        //                 else if (linkOverwrite == "true") //both user and originalUser are not null
        //                 {
        //                     await offlineRepo.DeleteUserAsync(originalUser.Id);
        //                     await securityManager.LinkUser(user.Id, originalProfile);
        //                 }
        //                 else
        //                 {
        //                     throw new BadUserInputException
        //                         ("overwriting is false and you're trying rewrite an existing user");
        //                 }
        //             }
        //         }
        //         //in case we upgrade from guest to facebook
        //
        //         if (user == null)
        //             user = await securityManager.SignUpAndInAsync(profile);
        //         else
        //             user = await securityManager.SignInAsync(profile, user);
        //
        //         persistantData.FeedScope(scopeRepo);
        //
        //         if (scopeRepo.IsUserPending(user.Id))
        //             throw new Exceptions.BadUserInputException();
        //
        //         if (Request.Path == "/connect" &&
        //             scopeRepo.DoesRoomUserExist(user.Id) && scopeRepo.RoomUser.Active == false)
        //             return AuthenticateResult.Fail("the user is playing in a room but tries to connect again");
        //         //todo change this behaviour to surrender the existing user because it's safer in case the user
        //         //todo is marked in room falsely always
        //
        //         var genericClaims = new List<Claim>
        //         {
        //             new(ClaimTypes.NameIdentifier, user.Id),
        //             new("betChoice", betChoice),
        //             new("capacityChoice", capacityChoice),
        //             //this is the identifier used in the signalr, this claim type "NameIdentifier" could be changed with IUserIdProvider
        //             //new Claim(ClaimTypes.Name, user.UserName),
        //             //new Claim(ClaimTypes.Email, user.Email),
        //             // new Claim("UserType", "General"),//role?
        //         }; //this the only claims I can obtain from the payload
        //
        //         if (Request.Path == "LinkTo")
        //         {
        //             genericClaims.Add(new Claim("provider", provider.ToString()));
        //             genericClaims.Add(new Claim("access_token", provider.ToString()));
        //             
        //         }
        //
        //         var genericIdentity = new ClaimsIdentity
        //             (genericClaims, /*Scheme.Name*/ authenticationType: PROVIDER_NAME);
        //         var principal = new GenericPrincipal(genericIdentity, roles: null);
        //         var ticket = new AuthenticationTicket(principal, Scheme.Name);
        //
        //         scopeRepo.SetRealOwner(user.Id);
        //
        //         logger.LogInformation("login succeeded for player: {UserId}", user.Id);
        //         return AuthenticateResult.Success(ticket);
        //     }
        //     catch (Exception exception)
        //     {
        //         return AuthenticateResult.Fail("auth exception with message: " + exception.Message);
        //     }
        //     //todo are you sure it's a bad request not internal server error?,
        //     //todo be more specific with errors and catch different user faults
        // }
        #endregion

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            //can have exceptions, they are caught here
            // try
            // {
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

            if (Request.Path == "/connect" &&
                (scopeRepo.IsUserPending() || scopeRepo.RoomUser?.Active == false))
                return AuthenticateResult.Fail("the user is playing in a room but tries to connect again");

            // return AuthenticateResult.Fail("");

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

            logger.LogInformation("login succeeded for player: {UserId}", user.Id);
            return AuthenticateResult.Success(ticket);
            // }
            // catch (Exception exception)
            // {
            //     return AuthenticateResult.Fail("auth exception with message: " + exception.Message);
            // }
            //todo are you sure it's a bad request not internal server error?,
            //todo be more specific with errors and catch different user faults
        }
    }
}