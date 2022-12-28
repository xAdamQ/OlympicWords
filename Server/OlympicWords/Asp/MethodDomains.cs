using System.Collections.Immutable;
using System.Reflection;
using OlympicWords.Controllers;
using OlympicWords.Filters;

namespace OlympicWords.Services;

public static class MethodDomains
{
    static MethodDomains()
    {
        var rpcMethods = typeof(RoomHub).GetMethods(BindingFlags.Public | BindingFlags.Instance);

        Rpcs = rpcMethods.Where(r => r.GetCustomAttribute<RpcDomainAttribute>() != null)
            .ToImmutableDictionary(r => r, r => r.GetCustomAttribute<RpcDomainAttribute>()!.Domain);

        var actionMethods = typeof(UserController).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Concat(typeof(LobbyController).GetMethods(BindingFlags.Public | BindingFlags.Instance));

        Actions = actionMethods.Where(r => r.GetCustomAttribute<ActionDomainAttribute>() != null)
            .ToImmutableDictionary(r => r, r => r.GetCustomAttribute<ActionDomainAttribute>()!.Domain);
    }

    public static ImmutableDictionary<MethodInfo, Type> Rpcs { get; }
    public static ImmutableDictionary<MethodInfo, Type> Actions { get; }
}