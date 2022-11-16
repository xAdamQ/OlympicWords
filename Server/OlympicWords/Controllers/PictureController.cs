using Microsoft.AspNetCore.Mvc;
using OlympicWords.Services;
using OlympicWords.Services.Extensions;

namespace OlympicWords.Controllers;

[Route("[controller]/[action]")]
[ApiController]
public class PictureController : ControllerBase
{
    private readonly IOfflineRepo offlineRepo;
    private readonly IHttpContextAccessor httpContextAccessor;
    public PictureController(IOfflineRepo offlineRepo, IHttpContextAccessor httpContextAccessor)
    {
        this.offlineRepo = offlineRepo;
        this.httpContextAccessor = httpContextAccessor;
    }

    //not used by the client
    [HttpGet]
    [ActionName(nameof(GetMyPicture))]
    public async Task<IActionResult> GetMyPicture()
    {
        var userId = httpContextAccessor.HttpContext.User.GetLoggedInUserId<string>();
        var imageBytes = await offlineRepo.GetUserPicture(userId);
        return File(imageBytes, "image/jpg");
    }

    [HttpGet]
    [ActionName(nameof(GetUserPicture))]
    public async Task<IActionResult> GetUserPicture(string userId)
    {
        var imageBytes = await offlineRepo.GetUserPicture(userId);
        return File(imageBytes, "image/jpg");
    }
}