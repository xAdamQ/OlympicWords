using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OlympicWords.Services;
using Shared;

namespace OlympicWords.Controllers;

[Route("[controller]/[action]")]
[ApiController]
[AllowAnonymous]
public class TestController : ControllerBase
{
    private readonly IOfflineRepo offlineRepo;

    public TestController(IOfflineRepo offlineRepo)
    {
        this.offlineRepo = offlineRepo;
    }

    [AllowAnonymous]
    public async Task Test()
    {
        var user = await offlineRepo.GetUserAsync("r31ecc3b-78fdb99b-4aafc-b364-680dfda0a4aq", ProviderType.Guest);
    }


    [AllowAnonymous]
    public async Task Test2()
    {
        var user = offlineRepo.TrackNewUser("47b95fb7-c727-494d-8929-ce1cd5fbab0c");
        user.Email = "hello the name is set";
        await offlineRepo.SaveChangesAsync();
    }

    [AllowAnonymous]
    public async Task Test3()
    {
        var user = offlineRepo.TrackNewUser("47b95fb7-c727-494d-8929-ce1cd5fbab0c");

        offlineRepo.ModifyUserProperty(user, u => u.EnableOpenMatches, false);
        offlineRepo.ModifyUserProperty(user, u => u.Email, "thank tou");

        await offlineRepo.SaveChangesAsync();
    }


    [AllowAnonymous]
    public async Task<ArraySegment<char>> Test4()
    {
        var str = "1234567890qwertyuiopasdfghjkl;Segmdent".ToCharArray();
        return new ArraySegment<char>(str);
    }


    [AllowAnonymous]
    public async Task<char[]> Test5()
    {
        return "1234567890qwertyuiopasdfghjkl;".ToCharArray();
    }
}