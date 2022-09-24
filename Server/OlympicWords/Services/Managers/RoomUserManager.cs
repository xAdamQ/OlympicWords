// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Basra.Server.Exceptions;
// using Basra.Server.Extensions;
// using Microsoft.AspNetCore.SignalR;
// using Microsoft.Extensions.Logging;
//
// namespace Basra.Server.Services
// {
//     public interface IRoomUserManager
//     {
//         Task RandomPlay(RoomUser roomUser);
//         Task Play(RoomUser roomUser, int cardIndexInHand);
//         void StartTurn(RoomUser roomUser);
//     }
//
//     /// <summary>
//     /// handle started room user initiated actions
//     /// the hub interact with this not room manager directly
//     /// </summary>
//     public class RoomUserManager : IRoomUserManager
//     {
//         private readonly IHubContext<MasterHub> _masterHub;
//         private readonly IRoomManager _roomManager;
//         private readonly IServerLoop _serverLoop;
//
//         public RoomUserManager(IHubContext<MasterHub> masterHub, IRoomManager roomManager, IServerLoop serverLoop)
//         {
//             _masterHub = masterHub;
//             _roomManager = roomManager;
//             _serverLoop = serverLoop;
//         }
//
//          }
// }

