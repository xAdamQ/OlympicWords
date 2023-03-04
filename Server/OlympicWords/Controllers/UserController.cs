using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OlympicWords.Common;
using OlympicWords.Data;
using OlympicWords.Services;
using OlympicWords.Services.Extensions;
using Shared;
using Shared.Controllers;

namespace OlympicWords.Controllers;

[Route("[controller]/[action]")]
[ApiController]
[Authorize]
public class UserController : ControllerBase, IUserController
{
    private readonly ILogger<UserController> logger;
    private readonly IOfflineRepo offlineRepo;
    private readonly IScopeRepo scopeRepo;
    private readonly HttpContext context;

    public UserController(ILogger<UserController> logger, IOfflineRepo offlineRepo,
        IHttpContextAccessor contextAccessor, IScopeRepo scopeRepo)
    {
        this.logger = logger;
        this.offlineRepo = offlineRepo;
        this.scopeRepo = scopeRepo;
        context = contextAccessor.HttpContext;
    }

    private string UserId => userId ??= context.User.GetLoggedInUserId<string>();
    private string userId;

    public async Task<PersonalFullUserInfo> Personal()
    {
        return await offlineRepo.GetPersonalInfo(track: false);
    }

    /// <summary>
    /// get public user data by his id
    /// </summary>
    public async Task<FullUserInfo> Public(string id)
    {
        var data = await offlineRepo.GetFullUserInfoAsync(id);
        data.Friendship = (int)await offlineRepo.GetFriendship(UserId, id);
        //it's possible to get the friendship from the same db call, I won't implement it now because
        //it's complex

        return data;
    }

    public async Task ToggleFollow(string targetId)
    {
        await offlineRepo.ToggleFollow(targetId);
        await offlineRepo.SaveChangesAsync();
    }

    public async Task SetOpenMatches(bool value)
    {
        var user = offlineRepo.GetEmptyCurrenUserTracked();
        // user.EnableOpenMatches = false;
        // offlineRepo.MarkUserPropertyModified(user, u => u.EnableOpenMatches);
        offlineRepo.ModifyUserProperty(user, u => u.EnableOpenMatches, value);
        await offlineRepo.SaveChangesAsync();
    }


    [AllowAnonymous]
    public async Task<IActionResult> LinkTo([FromServices] SecurityManager securityManager, string originalToken,
        string originalProviderString, string newProviderString, string newToken, bool overwriteNew)
    {
        var newProviderParsed = Enum.TryParse(newProviderString, out ProviderType newProvider);
        if (!newProviderParsed)
        {
            ModelState.AddModelError("wrong provider", "failed to parse the new provider");
            return BadRequest(ModelState);
        }

        var originalProviderParsed = Enum.TryParse(originalProviderString, out ProviderType originalProvider);
        if (!originalProviderParsed)
        {
            ModelState.AddModelError("wrong provider", "failed to parse the original provider");
            return BadRequest(ModelState);
        }

        var originalProfile = await securityManager.GetProfile(originalProvider, originalToken);
        var originalUser = await offlineRepo.GetUserAsync(originalToken, originalProvider);

        var newProfile = await securityManager.GetProfile(newProvider, newToken);
        var newUser = await offlineRepo.GetUserAsync(newToken, newProvider);

        if (newUser == null && originalUser != null)
        {
            await securityManager.LinkUser(originalUser.Id, newProfile);
        }
        else if (originalUser == null)
        {
            ModelState.AddModelError("link", "you're trying to link to a non-existing user");
            return BadRequest(ModelState);
        }
        //both user and originalUser are not null
        else if (overwriteNew)
        {
            offlineRepo.DeleteUserAsync(originalUser.Id);
            await offlineRepo.SaveChangesAsync();
            await securityManager.LinkUser(newUser.Id, originalProfile);
        }
        else
        {
            ModelState.AddModelError("link", "overwriting is false and you're trying rewrite an existing user");
            return BadRequest(ModelState);
        }

        return Ok();
    }
    //after calling that, you can re-request your personal data with the new provider
}