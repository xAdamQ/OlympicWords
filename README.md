# OlympicWords

## overview
This is a project I made to help me learn how multiplayer games work, and know the possible challenges that I can face in a practical manner. <br />
I chose ASP.NET Core SignalR for the backend because I wanted to make use of WebGL as it looks futuristic, allowing you to start playing directly and download parts of the game on demand, and make use of your GPU.
## what is this game?
Touch typing is a way that enables you to type on the keyboard without looking at it, this enables you to focus on what you are doing and let your brain automate the keystrokes with your muscle memory, so you can offload the typing part. This is similar to speaking while walking, you don't think of how to move your legs.
## how can I play
The game is available on a free/dev server that takes time to warm up so you may need to refresh the first couple of times. And it gives timeout errors and false CORS errors sometime. Try here: https://tuxul.com. Everything was hosted on a Linux VM and it was as fast as running locally, but I put everything under the corresponding Azure services.
## Technical details
### 1. player scopes
When you create a multiplayer game, you have to make a lot of validations to the user inputs, so the server won't break with unexpected inputs, this can come from client bugs or malicious users. I found a way to make fewer validations using Player Scopes/Domains.
```C#
[RpcDomain(typeof(UserDomain.Room.Finished))]
public void LeaveFinishedRoom()
{
    Context.Abort();
}
```
I don't have to make validations in a nonrelated context because the possibility of error here is narrow since I specified the scope previously.
```C#
[RpcDomain(typeof(UserDomain.Room.Init.GettingReady))]
public async Task Ready()
{
    await matchMaker.MakeRoomUserReadyRpc();
}

//you can set the power up whether you're ready or not yet
[RpcDomain(typeof(UserDomain.Room.Init))]
public void SetPowerUp([ValidRange(3)] int powerUp)
{
    scopeRepo.RoomActor.ChosenPowerUp = powerUp;
}
```
In this example, you can set the powerup before you start the game whether you're ready or not, so if your scope is `Init` or a sub-scope of `Init`, `SetPowerUp` can be called.<br />
Dotnet 7 has generic types for attributes that can enforce supported domain types at compile time but this project uses dotnet 6.<br />
The metadata is collected only once at application startup and the existing method info object on the Action/Hub filters is used for validation, so this is not only pretty but should be performant also!
Here is how we collect the metadata:
```C#
//this code runs at server startup only
public static class MethodDomains
{
    static MethodDomains()
    {
        var rpcMethods = typeof(RoomHub).GetMethods(BindingFlags.Public | BindingFlags.Instance);
        Rpcs = rpcMethods.Where(r => r.GetCustomAttribute<RpcDomainAttribute>() != null)
            .ToImmutableDictionary(r => r, r => r.GetCustomAttribute<RpcDomainAttribute>()!.Domain);

        var actionMethods = typeof(UserController).GetMethods(BindingFlags.Public | BindingFlags.Instance);
        Actions = actionMethods.Where(r => r.GetCustomAttribute<ActionDomainAttribute>() != null)
            .ToImmutableDictionary(r => r, r => r.GetCustomAttribute<ActionDomainAttribute>()!.Domain);
    }

    public static ImmutableDictionary<MethodInfo, Type> Rpcs { get; }
    public static ImmutableDictionary<MethodInfo, Type> Actions { get; }
}
public static class ParamRanges
{
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
```
Here is an example validation code:
```C#
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
        throw new BadUserInputException("value out of range and so on....");
}
```
### 2. Shared files with symlink
Unity project can't depend on a class library project, so you will have to export the dll each time you make a change, also you can't access Unity APIs from the class library. I found a solution for that using `Symbolic Links` or `Hard Links` which enables the same file to exist on multiple locations, whether pointing to the source file, or the source disk node. So I can make changes from the Unity project or the ASP project without caring about syncing them. <br />
Here is a practical example that makes editing the controller/hub methods show errors and notify us at compile time, rather than calling the methods blindly (the file is shared between Unity and ASP).
```C#
public interface IUserController : IController
{
    Task<PersonalFullUserInfo> Personal();
    Task<FullUserInfo> Public(string id);
    Task ToggleFollow(string targetId);
    Task ToggleOpenMatches();
    Task
#if !UNITY
        <IActionResult>
#endif
        LinkTo(
#if !UNITY
            [FromServices] SecurityManager securityManager,
#endif
            string originalToken, string originalProviderString, string newProviderStr, string newToken, bool overwriteNew);
}
```
### 3. Proxies
This plays with the shared interface example above, it makes the client write literally no code for new methods. And since this is an http call, the added milliseconds won't get noticed. This is a part of an actual class in the project:
```C#
public class ControllerProxy<T> : DispatchProxyAsync where T : class, IController
{
    public override async Task InvokeAsync(MethodInfo method, object[] args)
    {
        await NetManager.I.SendAsyncHTTP(address + method.Name, GetQueryParams(method, args));
    }
    public static T CreateProxy()
    {
        return Create<T, ControllerProxy<T>>();
    }
}
```
Unfortunately, this uses `System.Reflection.Emit`, and WebGL requires Ahead Of Time Compilation, so the proxy part won't work with WebGL, and you will implement the interface.

I will add more info later, thanks for reading.
