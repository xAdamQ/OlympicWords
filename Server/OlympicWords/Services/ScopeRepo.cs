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
        /// <summary>
        /// we don't remove pending rooms, TakePendingRoom excludes them
        /// </summary>
        Room TakePendingRoom(int category, string env);
        void DeleteRoom();

        void AddNewUser(RoomUser ru);
        void MarkUserPending();
        bool DoesRoomUserExist(string id);
        void RemoveRoomUser();
        RoomUser GetRoomUser(string id);

        public string UserId { get; }


        RoomActor RoomActor { get; }
        Room Room { get; }
        RoomUser RoomUser { get; }
        RoomBot RoomBot { get; }

        void Init(ConcurrentDictionary<int, Room> rooms,
            ConcurrentDictionary<string, RoomUser> activeRoomUsers,
            ConcurrentDictionary<(int, string), ConcurrentBag<Room>> pendingRooms,
            HashSet<string> pendingUsers, ref int lastRoomId);

        public void SetRealOwner(string userId);
        public bool IsRealOwner { get; }
        public void SetBotOwner(RoomBot roomBot);
        bool IsUserPending();
        void RemovePendingUser();
        void RemovePendingUser(string uid);
        /// <summary>
        /// when the context doesn't have a specific user
        /// </summary>
        void SetRoom(Room room);
    }

    /// <summary>
    /// it can be used in shared memory server later
    /// </summary>
    public class PersistantData
    {
        private readonly ConcurrentDictionary<int, Room> rooms = new();
        private readonly ConcurrentDictionary<string, RoomUser> roomUsers = new();
        private readonly ConcurrentDictionary<(int, string), ConcurrentBag<Room>> pendingRooms = new();
        private readonly HashSet<string> pendingUsers = new();

        #region pending room messages and notifications
        /// <summary>
        /// rooms here may not have the full amount of players, this list is used to check if
        /// we will start them or fill them with bots
        /// </summary>
        public Queue<(Room, DateTime)> InitiatedRooms { get; } = new();
        /// <summary>
        /// rooms here have the full amount of players, but not all of them are ready
        /// </summary>
        public Queue<(Room, DateTime)> UnreadyRooms { get; } = new();
        public Dictionary<Room, CancellationTokenSource> PendingRoomCancellations { get; } = new();

        public void CancelPendingRoomTimeout(Room room)
        {
            PendingRoomCancellations[room].Cancel();
            PendingRoomCancellations.Remove(room);
        }
        #endregion

        private int lastRoomId;

        public PersistantData()
        {
            for (var i = 0; i < Room.Bets.Length; i++)
                foreach (var env in OfflineRepo.GameConfig.EnvConfigs.Select(e => e.Name))
                    pendingRooms.TryAdd((i, env), new ConcurrentBag<Room>());
        }

        public void FeedScope(IScopeRepo scopeRepo)
        {
            scopeRepo.Init(rooms, roomUsers, pendingRooms, pendingUsers, ref lastRoomId);
        }
    }

    /// <summary>
    /// collection holder
    /// all it's operations are on collection
    /// </summary>
    public class ScopeRepo : IScopeRepo
    {
        #region props
        /// <summary>
        /// called only if the user inside the room, otherwise it will be null
        /// </summary>
        public RoomActor RoomActor
        {
            get
            {
                if (IsRealOwner) return RoomUser;

                return RoomBot;
            }
        }

        //idk if casting everytime is expensive
        public RoomUser RoomUser
        {
            get
            {
                if (!IsRealOwner)
                    throw new Exception("RoomBot is the owner, but you need a room user");

                if (roomUser != null) return roomUser;

                return roomUser = GetRoomUserWithId(UserId);
            }
        }
        private RoomUser roomUser;

        public RoomBot RoomBot { get; private set; }

        /// <summary>
        /// if the room user is null, this is null
        /// </summary>
        public Room Room
        {
            get => room ??= RoomActor?.Room;
            set => room = value;
        }
        private Room room;

        private readonly ILogger<ScopeRepo> logger;

        private ConcurrentDictionary<int, Room> rooms;
        private ConcurrentDictionary<string, RoomUser> activeRoomUsers;
        private ConcurrentDictionary<(int, string), ConcurrentBag<Room>> pendingRooms;
        private HashSet<string> pendingUsers;
        private int lastRoomId;
        public string UserId { get; private set; }
        #endregion

        public ScopeRepo(ILogger<ScopeRepo> logger)
        {
            this.logger = logger;
        }

        public void Init(ConcurrentDictionary<int, Room> rooms,
            // ConcurrentDictionary<string, ActiveUser> activeUsers,
            ConcurrentDictionary<string, RoomUser> activeRoomUsers,
            ConcurrentDictionary<(int, string), ConcurrentBag<Room>> pendingRooms,
            HashSet<string> pendingUsers, ref int lastRoomId)
        {
            this.rooms = rooms;
            // this.activeUsers = activeUsers;
            this.activeRoomUsers = activeRoomUsers;
            this.pendingRooms = pendingRooms;
            this.lastRoomId = lastRoomId;
            this.pendingUsers = pendingUsers;
        }

        public void SetRealOwner(string userId)
        {
            UserId = userId;
            IsRealOwner = true;
        }
        public void SetBotOwner(RoomBot roomBot)
        {
            RoomBot = roomBot;
            UserId = roomBot.Id;
            IsRealOwner = false;
        }
        /// <summary>
        /// when the context doesn't have a specific user
        /// </summary>
        public void SetRoom(Room room)
        {
            Room = room;
        }

        /// <summary>
        /// decides if the owner is roomBot or roomUser
        /// </summary>
        public bool IsRealOwner { get; private set; }

        public void DeleteRoom()
        {
            rooms.TryRemove(Room.Id, out _);
            room.CancellationTokenSource.Cancel();
            Room.IsDeleted = true;
        }

        /// <summary>
        /// if the room is still pending 
        /// </summary>
        public Room AddRoom(Room room)
        {
            var newId = Interlocked.Increment(ref lastRoomId);
            room.Id = newId;
            rooms.TryAdd(newId, room);
            return room;
        }
        /// <summary>
        /// takes possible rooms, excludes active amd null rooms
        /// </summary>
        public Room TakePendingRoom(int category, string env)
        {
            var bag = pendingRooms[(category, env)];

            while (!bag.IsEmpty)
            {
                bag.TryTake(out var r);

                if (r is { IsFull: false, IsDeleted: false }) return r;
            }

            return null;
        }
        public void KeepPendingRoom(Room room)
        {
            pendingRooms[(room.Category, room.Env)].Add(room);
            // PendingRooms[(room.BetChoice, room.CapacityChoice)].Enqueue(room);
        }

        private RoomUser GetRoomUserWithId(string id)
        {
            activeRoomUsers.TryGetValue(id, out var ru);
            return ru;
        }
        public bool DoesRoomUserExist(string id)
        {
            return activeRoomUsers.ContainsKey(id);
        }
        public void RemoveRoomUser()
        {
            RoomUser.Cancellation.Cancel();
            activeRoomUsers.TryRemove(RoomUser.Id, out _);
        }
        public RoomUser GetRoomUser(string id)
        {
            return activeRoomUsers[id];
        }
        public void AddNewUser(RoomUser ru)
        {
            activeRoomUsers.TryAdd(ru.Id, ru);
        }
        public void MarkUserPending()
        {
            pendingUsers.Add(UserId);
        }
        public bool IsUserPending()
        {
            return pendingUsers.Contains(UserId);
        }
        public void RemovePendingUser()
        {
            pendingUsers.Remove(UserId);
        }
        public void RemovePendingUser(string uid)
        {
            pendingUsers.Remove(uid);
        }


        // public ActiveUser GetActiveUser(string id)
        // {
        //     activeUsers.TryGetValue(id, out var au);
        //     return au;
        // }
        // public bool IsUserActive(string id)
        // {
        //     return activeUsers.ContainsKey(id);
        // }
        // public void RemoveActiveUser(string id)
        // {
        //     var result = activeUsers.TryRemove(id, out _);
        //
        //     if (!result) logger.LogWarning("removing active user was id {Id} failed", id);
        // }
        // public void AddActiveUser(ActiveUser activeUser)
        // {
        //     var result = activeUsers.TryAdd(activeUser.Id, activeUser);
        //
        //     if (!result) logger.LogWarning("adding active user was id {ActiveUserId} failed", activeUser.Id);
        // }
    }
}