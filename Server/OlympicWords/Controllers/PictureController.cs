using Microsoft.AspNetCore.Mvc;
using OlympicWords.Services;
using OlympicWords.Services.Extensions;

namespace OlympicWords.Controllers;

[Route("[controller]")]
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

    [HttpGet]
    public async Task<IActionResult> GetUserPicture()
    {
        var userId = httpContextAccessor.HttpContext.User.GetLoggedInUserId<string>();
        var imageBytes = await offlineRepo.GetUserPicture(userId);
        return File(imageBytes, "image/jpg");
    }
}