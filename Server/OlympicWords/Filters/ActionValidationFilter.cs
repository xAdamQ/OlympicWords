using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using OlympicWords.Services;
using OlympicWords.Services.Extensions;

namespace OlympicWords.Filters;

/// <summary>
/// this is a validation filter, rather than a domain only filter used in the hub
/// </summary>
public class ActionValidationFilter : IResourceFilter
{
    private readonly IScopeRepo scopeRepo;
    private readonly ILogger<ActionValidationFilter> logger;
    private readonly PersistantData persistantData;

    public ActionValidationFilter(IScopeRepo scopeRepo,
        ILogger<ActionValidationFilter> logger, PersistantData persistantData)
    {
        this.scopeRepo = scopeRepo;
        this.logger = logger;
        this.persistantData = persistantData;
    }

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var userId = context.HttpContext.User.GetLoggedInUserId<string>();

        //these seps are essential regardless of the validation, so this should be moved to a different filter that runs
        //before this one   
        persistantData.FeedScope(scopeRepo);
        scopeRepo.SetRealOwner(userId);

        var descriptor = (ControllerActionDescriptor)context.ActionDescriptor!;
        MethodDomains.Actions.TryGetValue(descriptor.MethodInfo, out var domain);

        //we don't have domain rules for this method
        if (domain == null)
            return;

        //this validation is general so it good
        if (domain.CompareDomains<UserDomain.Stateless>())
        {
            if (scopeRepo.DoesRoomUserExist(userId))
            {
                context.Result = new BadRequestResult();
                context.ModelState.AddModelError("Domain", "you can't make a stateless request with an active user");

                return;
            }

            //but this validation is specific to a single function for now, however, it can expand in the future
            if (domain.CompareDomains<UserDomain.Stateless>())
            {
                if (!scopeRepo.IsUserPending())
                {
                    context.Result = new BadRequestResult();
                    context.ModelState.AddModelError
                        ("Domain", "you can't make a stateless request with an active user");
                }
            }
        }

        //we must not reach this part with the current design, because stateless is domain-less!
        //and there is nothing to attach the domain to unless I create a store for that like the pending store
        //put the pending store lives for the duration of the request

        // if (!activeUser.Domain.IsSubclassOf(domain) && !activeUser.Domain.IsEquivalentTo(domain))
        // {
        //     context.Result = new BadRequestResult();
        //     context.ModelState.AddModelError("Domain",
        //         $"the called action with domain {domain} is not valid in the current user domain {activeUser.Domain}");
        // }
    }


    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}