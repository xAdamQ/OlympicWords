using System;
using System.Reflection;
using System.Threading.Tasks;
using Shared.Controllers;

public class UserController : ControllerBase, IUserController
{
    public Task<PersonalFullUserInfo> Personal()
    {
        return GetAsync<PersonalFullUserInfo>(nameof(Personal));
    }

    public Task<FullUserInfo> Public(string id)
    {
        return GetAsync<FullUserInfo>(nameof(Personal), (nameof(id), id));
    }

    public Task ToggleFollow(string targetId)
    {
        return SendAsync(nameof(ToggleFollow), (nameof(targetId), targetId));
    }

    public Task ToggleOpenMatches()
    {
        return SendAsync(nameof(ToggleOpenMatches));
    }

    public Task LinkTo(string originalToken, string originalProviderString, string newProviderStr,
        string newToken, bool overwriteNew)
    {
        return SendAsync(nameof(LinkTo),
            (nameof(originalToken), originalToken),
            (nameof(originalProviderString), originalProviderString),
            (nameof(newProviderStr), newProviderStr),
            (nameof(newToken), newToken),
            (nameof(overwriteNew), overwriteNew.ToString()));
    }
}