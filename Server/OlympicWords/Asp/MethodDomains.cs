using System.Collections.Immutable;
using System.Reflection;
using Castle.DynamicProxy.Generators;
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
public static class ParamRanges
{
    //this code runs at server startup only
    static ParamRanges()
    {
        var allMethods = typeof(RoomHub).GetMethods(BindingFlags.Public | BindingFlags.Instance);

        Values = allMethods
            .Select(m => m.GetParameters()).SelectMany(p => p)
            .Select(p => (ParameterInfo: p, Attribute: p.GetCustomAttribute<ValidRange>()))
            .Where(p => p.Attribute != null)
            .ToImmutableDictionary(p => p.ParameterInfo, p => (p.Attribute.Min, p.Attribute.Max));
    }

    public static ImmutableDictionary<ParameterInfo, (int min, int max)> Values { get; }
}