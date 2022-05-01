// using System.Reflection;
// using System.Runtime.CompilerServices;
// using Microsoft.AspNetCore.SignalR;
//
// namespace OlympicWords;
//
// public class MasterHub : Hub
// {
//     private readonly IRoomManager roomManager;
//     private readonly IScopeInfo scopeInfo;
//     private readonly ILogger<MasterHub> logger;
//
//     public MasterHub(IRoomManager roomManager, IScopeInfo scopeInfo, ILogger<MasterHub> logger)
//     {
//         this.roomManager = roomManager;
//         this.roomManager = roomManager;
//         this.scopeInfo = scopeInfo;
//         this.logger = logger;
//     }
//
//     public override Task OnConnectedAsync()
//     {
//         logger.LogInformation($"a new user is connected {Context.ConnectionId}");
//
//         return base.OnConnectedAsync();
//     }
//
//
//
//
//
//
//     public class MethodDomains
//     {
//         public MethodDomains()
//         {
//             var rpcs =
//                 typeof(MasterHub).GetMethods(BindingFlags.Public | BindingFlags.Instance);
//
//             foreach (var rpc in rpcs)
//             {
//                 var attribute = rpc.GetCustomAttribute<RpcDomainAttribute>();
//
//                 if (attribute == null) continue;
//
//                 Domains.Add(rpc.Name, attribute.Domain);
//             }
//         }
//
//         private Dictionary<string, Type> Domains { get; } = new();
//
//         public Type GetDomain(string method)
//         {
//             return !Domains.ContainsKey(method) ? null : Domains[method];
//             // throw new Exception("the request function is not listed in the hub public methods");
//         }
//     }
//
//     [AttributeUsage(AttributeTargets.Method, Inherited = false)]
//     private sealed class RpcDomainAttribute : Attribute
//     {
//         public Type Domain { get; }
//
//         public RpcDomainAttribute(Type domain)
//         {
//             Domain = domain;
//         }
//     }
// }

