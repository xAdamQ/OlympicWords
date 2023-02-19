    using System;
using System.Reflection;
using System.Threading.Tasks;
using OlympicWords.Services.Exceptions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace OlympicWords.Services
{
    public class BadUserInputFilter : IHubFilter
    {
        private readonly IScopeRepo scopeRepo;
        private readonly ILogger<BadUserInputFilter> logger;
        private readonly PersistantData persistantData;

        public BadUserInputFilter(IScopeRepo scopeRepo,
            ILogger<BadUserInputFilter> logger, PersistantData persistantData)
        {
            this.scopeRepo = scopeRepo;
            this.logger = logger;
            this.persistantData = persistantData;
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

            logger.LogInformation("Calling hub method '{Method}' with args {Args}",
                invocationContext!.HubMethodName,
                string.Join(", ", invocationContext!.HubMethodArguments));

            persistantData.FeedScope(scopeRepo);
            scopeRepo.SetRealOwner(invocationContext.Context.UserIdentifier);

            var roomUser = scopeRepo.RoomUser;
            var domain = MethodDomains.Rpcs[invocationContext.HubMethod];

            if (domain == null)
            {
                throw new Exceptions.BadUserInputException(
                    "the user is invoking a function that doesn't exist or it's not an rpc");
            }

            if (!roomUser.Domain.IsSubclassOf(domain) &&
                !roomUser.Domain.IsEquivalentTo(domain))
            {
                throw new Exceptions.BadUserInputException(
                    $"the called function with domain {domain} is not valid in the current user domain {roomUser.Domain}");
            }

            var rangeAttributes = invocationContext.HubMethod.GetParameters()
                .Where(p => p.ParameterType == typeof(int))
                .Select(p => (SequencePosition: p.Position, Attribute: p.GetCustomAttribute<ValidRange>()))
                .Where(p => p.Attribute != null);

            foreach (var (pos, att) in rangeAttributes)
            {
                var arg = (int)invocationContext.HubMethodArguments[pos];
                if (arg > att.Max || arg < att.Min)
                    throw new BadUserInputException(" out of range");
            }

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