using OlympicWords.Services.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace OlympicWords.Services
{
    public interface IScopeRepo
    {
        Room AddRoom(Room room);

        /// <summary>
        /// if the room is still pending 
        /// </summary>
        void KeepPendingRoom(Room room);

        Room TakePendingRoom(int category, int capacityChoice);
        //important: we don't remove pending rooms, TakePendingRoom excludes them

        void DeleteRoom();

        void AddRoomUser(RoomUser roomUser);

        bool DoesRoomUserExist(string id);

        void RemoveRoomUser();


        RoomActor RoomActor { get; }
        ActiveUser ActiveUser { get; }
        Room Room { get; }
        RoomUser RoomUser { get; }
        RoomBot RoomBot { get; }

        ActiveUser GetActiveUser(string id);
        bool IsUserActive(string id);
        void RemoveActiveUser(string id);
        void AddActiveUser(ActiveUser activeUser);

        void Init(ConcurrentDictionary<int, Room> rooms,
            ConcurrentDictionary<string, ActiveUser> activeUsers,
            ConcurrentDictionary<string, RoomUser> roomUsers,
            ConcurrentDictionary<(int, int), ConcurrentBag<Room>> pendingRooms,
            int lastRoomId);

        void SetOwner(string userId = null, RoomBot roomBot = null);
    }


    public class PersistantData
    {
        private ConcurrentDictionary<int, Room> rooms;
        private ConcurrentDictionary<string, ActiveUser> activeUsers;
        private ConcurrentDictionary<string, RoomUser> roomUsers;
        private ConcurrentDictionary<(int, int), ConcurrentBag<Room>> pendingRooms;

        private int lastRoomId;

        public PersistantData()
        {
            rooms = new ConcurrentDictionary<int, Room>();
            roomUsers = new ConcurrentDictionary<string, RoomUser>();
            activeUsers = new ConcurrentDictionary<string, ActiveUser>();

            pendingRooms = new ConcurrentDictionary<(int, int), ConcurrentBag<Room>>();

            for (var i = 0; i < Room.Bets.Length; i++)
            {
                for (var j = 0; j < Room.Capacities.Length; j++)
                {
                    pendingRooms.TryAdd((i, j), new ConcurrentBag<Room>());
                }
            }
        }

        public void FeedScope(IScopeRepo scopeRepo)
        {
            scopeRepo.Init(rooms, activeUsers, roomUsers, pendingRooms, lastRoomId);
        }
    }

    /// <summary>
    /// collection holder
    /// all it's operations are on collection
    /// </summary>
    public class ScopeRepo : IScopeRepo
    {
        /// <summary>
        /// decides if the owner is roomBot or roomUser
        /// </summary>
        private bool RealOwner;

        /// <summary>
        /// called only if the user inside the room, otherwise it will be null
        /// </summary>
        public RoomActor RoomActor
        {
            get
            {
                if (RealOwner)
                    return RoomUser;

                return RoomBot;
            }
        }

        //idk if casting everytime is expensive
        public RoomUser RoomUser
        {
            get
            {
                if (roomUser != null)
                    return roomUser;

                return roomUser = GetRoomUserWithId(userId);
            }
        }

        private RoomUser roomUser;

        public RoomBot RoomBot { get; private set; }

        public ActiveUser ActiveUser
        {
            get
            {
                if (activeUser != null)
                    return activeUser;

                activeUser = GetActiveUser(userId);
                return activeUser;
            }
        }

        private ActiveUser activeUser;


        /// <summary>
        /// if the room user is null, this is null
        /// </summary>
        public Room Room => RoomActor?.Room;

        private Room room;

        private readonly ILogger<ScopeRepo> logger;

        private ConcurrentDictionary<int, Room> rooms;
        private ConcurrentDictionary<string, ActiveUser> activeUsers;
        private ConcurrentDictionary<string, RoomUser> roomUsers;
        private ConcurrentDictionary<(int, int), ConcurrentBag<Room>> pendingRooms;
        private int lastRoomId;

        private string userId;

        public ScopeRepo(ILogger<ScopeRepo> logger)
        {
            this.logger = logger;
        }

        public void Init(ConcurrentDictionary<int, Room> rooms,
            ConcurrentDictionary<string, ActiveUser> activeUsers,
            ConcurrentDictionary<string, RoomUser> roomUsers,
            ConcurrentDictionary<(int, int), ConcurrentBag<Room>> pendingRooms,
            int lastRoomId)
        {
            this.rooms = rooms;
            this.activeUsers = activeUsers;
            this.roomUsers = roomUsers;
            this.pendingRooms = pendingRooms;
            this.lastRoomId = lastRoomId;
        }

        public void SetOwner(string userId = null, RoomBot roomBot = null)
        {
            this.userId = userId;
            this.RoomBot = roomBot;

            RealOwner = userId != null;
        }

        public void DeleteRoom()
        {
            rooms.TryRemove(room.Id, out _);
        }


        public void RemoveRoomUser()
        {
            RoomUser.Cancellation.Cancel();
            roomUsers.TryRemove(RoomUser.Id, out _);
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

        private RoomUser GetRoomUserWithId(string id)
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