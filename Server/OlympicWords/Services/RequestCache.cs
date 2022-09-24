// using System;
// using System.Threading.Tasks;
//
// namespace OlympicWords.Services
// {
//     public interface IRequestCache
//     {
//         void Init(string userId);
//         RoomUser RoomUser { get; }
//         ActiveUser ActiveUser { get; }
//         Task<User> GetUser();
//     }
//
//     public class RequestCache : IRequestCache
//     {
//         private readonly IOfflineRepo offlineRepo;
//         private readonly IOnlineRepo onlineRepo;
//         public RequestCache(IOfflineRepo offlineRepo, IOnlineRepo onlineRepo)
//         {
//             this.offlineRepo = offlineRepo;
//             this.onlineRepo = onlineRepo;
//         }
//
//         private string UserId { get; set; }
//
//         private bool inited;
//         public void Init(string userId)
//         {
//             if (inited) throw new Exception("the request cache can't be inited twice");
//             inited = true;
//
//             UserId = userId;
//         }
//
//         private RoomUser roomUser;
//         public RoomUser RoomUser => roomUser ??= onlineRepo.GetRoomUserWithId(UserId);
//
//         private User user;
//         public async Task<User> GetUser() => user ??= await offlineRepo.GetUserByIdAsyc(UserId);
//
//         private ActiveUser activeUser;
//         public ActiveUser ActiveUser => activeUser ??= onlineRepo.GetActiveUser(UserId);
//     }
// }