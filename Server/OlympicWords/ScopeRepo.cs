// using System.Collections.Concurrent;
// using System.Diagnostics.CodeAnalysis;
//
// namespace OlympicWords;
//
// public interface IOnlineRepo
// {
//     RoomUser GetRoomUser(string id);
//     void AddRoomUser(RoomUser roomUser);
//     void AddPendingRoom(Room room);
//     Room? TakePendingRoom(int playerCountChoice, int category);
// }
//
// public class OnlineRepo : IOnlineRepo
// {
//     private ConcurrentDictionary<string, RoomUser> RoomUsers { get; } = new();
//
//     private List<Room> PendingRooms { get; } = new();
//
//     public RoomUser GetRoomUser(string id)
//     {
//         RoomUsers.TryGetValue(id, out var user);
//
//         ArgumentNullException.ThrowIfNull(user);
//
//         return user;
//     }
//
//     public void AddRoomUser(RoomUser roomUser)
//     {
//         var success = RoomUsers.TryAdd(roomUser.Id, roomUser);
//
//         if (!success) throw new ArgumentException("room can't be added to the repo", nameof(roomUser));
//     }
//
//     // public Room GetRoom(int id)
//     // {
//     //     Rooms.TryGetValue(id, out var room);
//     //
//     //     ArgumentNullException.ThrowIfNull(room);
//     //
//     //     return room;
//     // }
//
//
//     /*
//      try take a pending room, if null, the caller make one and add it here if not final
//      try take a pending room, if not null the caller check if it became final or not to add back it or not
//      */
//     public void AddPendingRoom(Room room)
//     {
//         lock (PendingRooms)
//             PendingRooms.Add(room);
//     }
//
//     public Room? TakePendingRoom(int playerCountChoice, int category)
//     {
//         lock (PendingRooms)
//         {
//             return PendingRooms.TakeOrDefault(r =>
//                 r.Category == category && r.CapacityChoice == playerCountChoice);
//         }
//     }
// }
//
// public static partial class Extensions
// {
//     public static T Take<T>(this IList<T> list, int index)
//     {
//         var item = list[index];
//         list.RemoveAt(index);
//         return item;
//     }
//
//     public static T? TakeOrDefault<T>(this IList<T> list, Func<T, bool> predicate)
//     {
//         var item = list.FirstOrDefault(predicate);
//
//         if (item != null)
//             list.Remove(item);
//
//         return item;
//     }
// }

