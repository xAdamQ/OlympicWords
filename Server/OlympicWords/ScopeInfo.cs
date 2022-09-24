// using Microsoft.AspNetCore.SignalR;
// using OlympicWords.Services;
//
// namespace OlympicWords;
//
// public interface IScopeInfo
// {
//     RoomUser RoomUser { get; }
//     ActiveUser ActiveUser { get; }
//     void Init(string userId);
//     Room Room { get; }
// }
//
// public class ScopeInfo : IScopeInfo
// {
//     private IOnlineRepo OnlineRepo { get; }
//     private IHubContext<MasterHub> HubContext { get; }
//     private string UserId { get; set; }
//
//     /// <summary>
//     /// called only if the user inside the room, otherwise it will be null
//     /// </summary>
//     public RoomUser RoomUser
//     {
//         get
//         {
//             if (roomUser != null)
//                 return roomUser;
//
//             roomUser = OnlineRepo.GetRoomUserWithId(UserId);
//
//             // ArgumentNullException.ThrowIfNull(roomUser);
//             //nullable
//
//             return roomUser;
//         }
//     }
//
//     private RoomUser roomUser;
//
//     public ActiveUser ActiveUser
//     {
//         get
//         {
//             if (activeUser != null)
//                 return activeUser;
//
//             activeUser = OnlineRepo.GetActiveUser(UserId);
//             return activeUser;
//         }
//     }
//
//     private ActiveUser activeUser;
//
//     /// <summary>
//     /// if the room user is null, this is null
//     /// </summary>
//     public Room Room => RoomUser?.Room;
//
//     private Room room;
//
//     public ScopeInfo(IOnlineRepo onlineRepo, IHubContext<MasterHub> hubContext)
//     {
//         this.OnlineRepo = onlineRepo;
//         this.HubContext = hubContext;
//     }
//
//     public void Init(string userId)
//     {
//         this.UserId = userId;
//     }
// }

