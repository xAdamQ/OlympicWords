// using System.Collections.Concurrent;
// using OlympicWords.Services;
// using OlympicWords.Services.Extensions;
//
// namespace OlymbicWords;
//
// public interface IOnlineRepo
// {
//     Room AddRoom(Room room);
//
//
//     /// <summary>
//     /// if the room is still pending 
//     /// </summary>
//     void KeepPendingRoom(Room room);
//
//     Room TakePendingRoom(int category, int capacityChoice);
//     //important: we don't remove pending rooms, TakePendingRoom excludes them
//
//     void DeleteRoom(Room room);
//
//     void AddRoomUser(RoomUser roomUser);
//
//     bool DoesRoomUserExist(string id);
//
//     ActiveUser GetActiveUser(string id);
//     bool IsUserActive(string id);
//     void RemoveActiveUser(string id);
//     void AddActiveUser(ActiveUser activeUser);
// }
//
// public class OnlineRepo : IOnlineRepo
// {
//     private readonly ILogger<OnlineRepo> logger;
//
//     private ConcurrentDictionary<int, Room> rooms;
//     private ConcurrentDictionary<string, ActiveUser> activeUsers;
//     private ConcurrentDictionary<string, RoomUser> roomUsers;
//
//     private int lastRoomId;
//
//     private ConcurrentDictionary<(int, int), ConcurrentBag<Room>> pendingRooms;
//
//     public OnlineRepo(ILogger<OnlineRepo> logger)
//     {
//         this.logger = logger;
//
//         rooms = new ConcurrentDictionary<int, Room>();
//         roomUsers = new ConcurrentDictionary<string, RoomUser>();
//         activeUsers = new ConcurrentDictionary<string, ActiveUser>();
//
//         pendingRooms = new ConcurrentDictionary<(int, int), ConcurrentBag<Room>>();
//
//         for (var i = 0; i < Room.Bets.Length; i++)
//         {
//             for (var j = 0; j < Room.Capacities.Length; j++)
//             {
//                 pendingRooms.TryAdd((i, j), new ConcurrentBag<Room>());
//             }
//         }
//     }
//
//     /// <summary>
//     /// if the room is still pending 
//     /// </summary>
//     public Room AddRoom(Room room)
//     {
//         rooms.Append(ref lastRoomId, room);
//
//         return room;
//     }
// }

