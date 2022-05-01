using System;
using System.Threading.Tasks;
using OlympicWords.Services.Exceptions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace OlympicWords.Services
{
    public class BadUserInputFilter : IHubFilter
    {
        private readonly MasterHub.MethodDomains methodDomains;
        private readonly IOnlineRepo onlineRepo;
        private readonly ILogger<BadUserInputFilter> logger;
        private readonly IRequestCache requestCache;
        public BadUserInputFilter(MasterHub.MethodDomains methodDomains, IOnlineRepo onlineRepo,
            ILogger<BadUserInputFilter> logger, IRequestCache requestCache)
        {
            this.methodDomains = methodDomains;
            this.onlineRepo = onlineRepo;
            this.logger = logger;
            this.requestCache = requestCache;
        }

        public async ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext,
            Func<HubInvocationContext, ValueTask<object>> next)
        {
            //HubException from here can terminate the invokation before calling the hub method or any next, and the clietn will recieve an error

            //practical: you can add custom attribute on the method and fetch it's data from here
            //E.G:
            // var languageFilter = (LanguageFilterAttribute)Attribute.GetCustomAttribute(
            //             invocationContext.HubMethod, typeof(LanguageFilterAttribute));
            // if (languageFilter != null &&
            //     invocationContext.HubMethodArguments.Count > languageFilter.FilterArgument &&
            //     invocationContext.HubMethodArguments[languageFilter.FilterArgument] is string str)

            logger.LogInformation(
                $"Calling hub method '{invocationContext.HubMethodName}'" +
                $" with args {string.Join(", ", invocationContext.HubMethodArguments)}");

            var activeUser = onlineRepo.GetActiveUser(invocationContext.Context.UserIdentifier);
            var domain = methodDomains.GetDomain(invocationContext.HubMethodName);

            if (activeUser.IsDisconnected)
                throw new Exception(
                    "there's something wrong with ur sys, a user is disconnected and calling!");

            if (domain == null)
            {
                throw new Exceptions.BadUserInputException(
                    "the user is invoking a function that doesn't exist or it's not an rpc");
            }
            if (!activeUser.Domain.IsSubclassOf(domain) &&
                !activeUser.Domain.IsEquivalentTo(domain))
            {
                throw new Exceptions.BadUserInputException(
                    $"the called function with domain {domain} is not valid in the current user domain {activeUser.Domain}");
            }

            requestCache.Init(invocationContext.Context.UserIdentifier);

            try
            {
                return await next(invocationContext);
                // invokes the next filter. And if it's the last filter, invokes the hub method
            }
            catch (Exceptions.BadUserInputException)
            {
                logger.LogInformation("BadUserInputException happened");

                throw;
                // return new ValueTask<int>(1);
                // return new ValueTask<User>(new User {Name = "test name on the returned user"});
                // return new ValueTask<object>($"there's a buie exc on the server {e.Message}");
            }
            // catch (Exception ex)
            // {
            //     _logger.LogInformation($"Exception calling '{invocationContext.HubMethodName}': {ex}");
            //     throw;
            // }
            finally
            {
                //check againest the called funtion if it's a trigger
                //is it throw?
                //call the serverloop to create new scope to check for distribute or finalize
                //my issue is the place
                //so create a trigger system
            }
        }

        // Optional method
        public Task OnConnectedAsync(HubLifetimeContext context,
            Func<HubLifetimeContext, Task> next)
        {
            return next(context);
        }

        // Optional method
        public Task OnDisconnectedAsync(HubLifetimeContext context, Exception exception,
            Func<HubLifetimeContext, Exception, Task> next)
        {
            return next(context, exception);
        }
    }
}