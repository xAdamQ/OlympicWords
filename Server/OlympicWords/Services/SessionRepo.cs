using OlympicWords.Services.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace OlympicWords.Services
{
    public interface IOnlineRepo
    {
        Room AddRoom(Room room);

        /// <summary>
        /// if the room is still pending 
        /// </summary>
        void KeepPendingRoom(Room room);

        Room TakePendingRoom(int category, int capacityChoice);
        //important: we don't remove pending rooms, TakePendingRoom excludes them

        void DeleteRoom(Room room);

        void AddRoomUser(RoomUser roomUser);
        RoomUser GetRoomUserWithId(string id);
        bool DoesRoomUserExist(string id);

        ActiveUser GetActiveUser(string id);
        bool IsUserActive(string id);
        void RemoveActiveUser(string id);
        void AddActiveUser(ActiveUser activeUser);
        void DeleteRoomUser(RoomUser roomUser);
    }


    /// <summary>
    /// collection holder
    /// all it's operations are on collection
    /// </summary>
    public class OnlineRepo : IOnlineRepo
    {
        private readonly ILogger<OnlineRepo> logger;

        //you may have room user that's not active
        //and you may have active user that's not in a room
        //if I made api endpoints before rooms, I will make the connection everytime but it maybe better, I should know
        //the overhead of a connected user
        //so instead of MasterHub, it would be RoomHub
        // private ConcurrentDictionary<string, ActiveUser> ActiveUsers;
        private ConcurrentDictionary<int, Room> rooms;
        private ConcurrentDictionary<string, ActiveUser> activeUsers;
        private ConcurrentDictionary<string, RoomUser> roomUsers;

        private int lastRoomId;

        private ConcurrentDictionary<(int, int), ConcurrentBag<Room>> pendingRooms;

        // private readonly int[] GenrePosses = {0, 1, 2, 3};
        private readonly int[] userCountPosses = {2, 3, 4};

        public OnlineRepo(ILogger<OnlineRepo> logger)
        {
            this.logger = logger;
            rooms = new ConcurrentDictionary<int, Room>();
            roomUsers = new ConcurrentDictionary<string, RoomUser>();
            activeUsers = new ConcurrentDictionary<string, ActiveUser>();

            pendingRooms = new ConcurrentDictionary<(int, int), ConcurrentBag<Room>>();

            for (var i = 0; i < Room.Bets.Length; i++)
            {
                for (var j = 0; j < userCountPosses.Length; j++)
                {
                    pendingRooms.TryAdd((i, j), new ConcurrentBag<Room>());
                }
            }
        }


        public void DeleteRoom(Room room)
        {
            rooms.TryRemove(room.Id, out _);
        }

        public void DeleteRoomUser(RoomUser roomUser)
        {
            roomUsers.TryRemove(roomUser.Id, out _);
        }

        /// <summary>
        /// if the room is still pending 
        /// </summary>
        public Room AddRoom(Room room)
        {
            rooms.Append(ref lastRoomId, room);

            return room;
        }

        /// <summary>
        /// takes possible rooms, excludes active amd null rooms
        /// </summary>
        public Room TakePendingRoom(int category, int capacityChoice)
        {
            var bag = pendingRooms[(category, capacityChoice)];

            while (true)
            {
                if (bag.IsEmpty) return null;

                // bag.TryDequeue(out Room room);
                bag.TryTake(out var room);

                if (room != null && !room.IsFull) return room;
            }
        }

        public void KeepPendingRoom(Room room)
        {
            pendingRooms[(room.Category, room.CapacityChoice)].Add(room);
            // PendingRooms[(room.BetChoice, room.CapacityChoice)].Enqueue(room);
        }

        public void AddRoomUser(RoomUser roomUser)
        {
            roomUsers.TryAdd(roomUser.Id, roomUser);
        }

        public RoomUser GetRoomUserWithId(string id)
        {
            roomUsers.TryGetValue(id, out var roomUser);
            return roomUser;
        }

        public bool DoesRoomUserExist(string id)
        {
            return roomUsers.ContainsKey(id);
        }
        // public void StartRoomUser(RoomUser roomUser, int turnId, string roomId)
        // {
        //     roomUser.Id = turnId;
        //     roomUser.RoomId = roomId;
        //     
        //     
        // }

        // public bool CheckRoomUserActive(string id)
        // {
        //     return RoomUsers.ContainsKey(id);
        // }
        //
        // public void RemoveRoomUser(string id)
        // {
        //     RoomUsers.TryRemove(id, out _);
        // }

        public ActiveUser GetActiveUser(string id)
        {
            activeUsers.TryGetValue(id, out var activeUser);
            return activeUser;
        }

        public bool IsUserActive(string id)
        {
            return activeUsers.ContainsKey(id);
        }

        public void RemoveActiveUser(string id)
        {
            var result = activeUsers.TryRemove(id, out _);

            if (!result) logger.LogWarning($"removing active user was id {id} faild");
        }

        public void AddActiveUser(ActiveUser activeUser)
        {
            var result = activeUsers.TryAdd(activeUser.Id, activeUser);

            if (!result) logger.LogWarning($"adding active user was id {activeUser.Id} faild");
        }
    }
}