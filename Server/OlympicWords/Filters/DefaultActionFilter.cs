using Microsoft.AspNetCore.Mvc.Filters;
using OlympicWords.Services;
using OlympicWords.Services.Extensions;

namespace OlympicWords.Filters;

// ReSharper disable once ClassNeverInstantiated.Global
public class DefaultActionFilter : IActionFilter
{
    private readonly PersistantData persistantData;
    private readonly IScopeRepo scopeRepo;
    public DefaultActionFilter(PersistantData persistantData, IScopeRepo scopeRepo)
    {
        this.persistantData = persistantData;
        this.scopeRepo = scopeRepo;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        persistantData.FeedScope(scopeRepo);

        var userId = context.HttpContext.User.GetLoggedInUserId<string>();
        scopeRepo.SetRealOwner(userId);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}