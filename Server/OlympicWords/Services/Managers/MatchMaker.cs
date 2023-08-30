using OlympicWords.Common;
using OlympicWords.Services.Extensions;
using Microsoft.AspNetCore.SignalR;
using OlympicWords.Services.Helpers;


/*
 *** user deletion flow
 1. when pending
    remove from pendingUsers, from it's room, from persistantData, delete room is empty
    discard deleted when fetching pending

2. when getting ready
3. in game
    we tread both the same, we let the game start, and notify the bots that the game room is cancelled so they stop producing letters
    
*/
namespace OlympicWords.Services
{
    public interface IMatchMaker
    {
        Task RequestRandomRoom(int category, string env);

        /// <summary>
        /// called by timeout
        /// </summary>
        Task FillPendingRoomWithBots();

        Task MakeRoomUserReadyRpc();
        void RemovePendingDisconnectedUser(RoomUser roomUser);
    }

    public class RoomPrepareResponse
    {
        public List<FullUserInfo> TurnSortedUsersInfo { get; set; }
        public List<string> SelectedItemPlayers { get; set; }
        public int TurnIndex { get; set; }
        public string Text { get; set; }
        public int Seed { get; set; }
        public List<(int index, int player)> FillerWords { get; set; }
        public List<int> ChosenPowerUps { get; set; }
    }

    public class MatchMaker : IMatchMaker
    {
        private readonly IHubContext<RoomHub> masterHub;
        private readonly IGameplay gameplay;
        private readonly ILogger<MatchMaker> logger;
        private readonly PersistantData persistantData;
        private readonly IOfflineRepo offlineRepo;
        private readonly IScopeRepo scopeRepo;

        public MatchMaker(IHubContext<RoomHub> masterHub, IOfflineRepo offlineRepo,
            IScopeRepo scopeRepo, IGameplay gameplay, ILogger<MatchMaker> logger, PersistantData persistantData)
        {
            this.masterHub = masterHub;
            this.offlineRepo = offlineRepo;
            this.scopeRepo = scopeRepo;
            this.gameplay = gameplay;
            this.logger = logger;
            this.persistantData = persistantData;
        }

        public async Task RequestRandomRoom(int category, string env)
        {
            //I don't yet have a room user, so I mark the id as pending, without domain
            scopeRepo.MarkUserPending();

            if (!category.IsInRange(Room.Bets.Length))
            {
                scopeRepo.RemovePendingUser();
                throw new Exceptions.BadUserInputException(
                    $"the room category: {category} exceeds the category list: {Room.Bets.Length}");
            }

            if (OfflineRepo.GameConfig.EnvConfigs.All(c => c.Name != env))
            {
                scopeRepo.RemovePendingUser();
                throw new Exceptions.BadUserInputException($"the environment {env} doesn't exist");
            }

            var dUser = await offlineRepo.GetCurrentUserAsync();

            if (dUser.Money < Room.Bets[category])
            {
                scopeRepo.RemovePendingUser();
                throw new Exceptions.BadUserInputException(
                    $"user doesn't have enough money{dUser.Money} for the bet {Room.Bets[category]}");
            }

            var room = scopeRepo.TakePendingRoom(category, env) ?? MakeRoom(category, env);
            var roomUser = CreateRoomUser(room);
            room.RoomUsers.Add(roomUser);
            room.RoomActors.Add(roomUser);

            // foreach (var disconnectedUser in room.RoomUsers.Where(ru => ru.Active))
            // RemovePendingDisconnectedUser(disconnectedUser);
            //the only way to tell the user is not active is by OnDisconnectedAsync which calls this method

            if (room.IsFull)
            {
                persistantData.CancelPendingRoomTimeout(room);
                await PrepareRoom(room);
            }
            else
            {
                scopeRepo.KeepPendingRoom(room); //so other users can see it
                persistantData.InitiatedRooms.Enqueue((room, DateTime.Now));
            }
        }

        /// <summary>
        /// called by timeout
        /// </summary>
        public async Task FillPendingRoomWithBots()
        {
            var room = scopeRepo.Room;

            var botsCount = room.Capacity - room.RoomUsers.Count;

            var botIds = new List<string> { "999", "9999", "99999" };
            for (var i = 0; i < botsCount; i++)
            {
                var botId = botIds.Cut(StaticRandom.GetRandom(botIds.Count));
                room.Bots.Add(new RoomBot(botId, room));
            }

            room.RoomActors.AddRange(room.Bots);

            await PrepareRoom(room);
        }

        private Room MakeRoom(int capacityChoice, string env)
        {
            return scopeRepo.AddRoom(new Room(capacityChoice, env));
        }

        public async Task MakeRoomUserReadyRpc()
        {
            scopeRepo.RoomUser.Domain = typeof(UserDomain.Room.Init.Ready);
            scopeRepo.RoomUser.IsReady = true;

            await StartRoomIfAllReady(scopeRepo.Room);
        } //doesn't fit into unit testing

        private async Task PrepareRoom(Room room)
        {
            room.SetUsersDomain<UserDomain.Room.Init.GettingReady>();

            room.RoomActors.Shuffle();
            for (var i = 0; i < room.RoomActors.Count; i++) room.RoomActors[i].TurnId = i;

            room.Text = offlineRepo.ChooseText(room.Category);
            room.Words = room.Text.Split(" ").ToList();

            SetFillers(room);

            var userIds = room.RoomActors.Select(ru => ru.Id).ToList();

            var users = await offlineRepo.GetUsersAsync(userIds);

            users.ForEach(u => u.Money -= room.Bet);

            var turnSortedUsers = room.RoomActors
                .Join(users, actor => actor.Id, user => user.Id, (_, user) => user)
                .ToList();


            var usersInfos = turnSortedUsers.Select(u => OfflineRepo.Mapper.Map<FullUserInfo>(u)).ToList();
            var selectedItemPlayers = turnSortedUsers.Select(u => u.SelectedItemPlayer[room.Env]).ToList();

            room.RoomUsers.ForEach(ru => scopeRepo.RemovePendingUser(ru.Id));

            await offlineRepo.SaveChangesAsync();
            persistantData.UnreadyRooms.Enqueue((room, DateTime.Now));
            await SendPrepareRoom(room, usersInfos, selectedItemPlayers);
        }

        private IEnumerable<string> ChooseFillers()
        {
            var combinationType = StaticRandom.GetRandom(4);
            // var res = new List<string>(3); //max cap is 3

            switch (combinationType)
            {
                case 0: //3s
                    for (var i = 0; i < 3; i++)
                        yield return offlineRepo.SmallFillers.GetRandom();
                    break;
                case 1: //1s1m
                    yield return offlineRepo.SmallFillers.GetRandom();
                    yield return offlineRepo.MediumFillers.GetRandom();
                    break;
                case 2: //1l
                    yield return offlineRepo.LargeFillers.GetRandom();
                    break;
            }
        }

        public void SetFillers(Room room)
        {
            const int FILLER = (int)PowerUp.Filler;

            var allFillers = new List<(int player, string fillerText)>();
            var fillerPlayers = room.RoomActors.Where(a => a.ChosenPowerUp == FILLER).ToList();
            foreach (var fillerPLayer in fillerPlayers)
                allFillers.AddRange(ChooseFillers().Select(f => (fillerPLayer.TurnId, f)));

            for (var i = 0; i < allFillers.Count; i++)
            {
                var words = allFillers[i].fillerText.Split(" ");
                var index = StaticRandom.GetRandom(room.Words.Count - 1);

                room.Words.InsertRange(index + 1, words);

                //push others, if we already filled the new, to avoid affecting the new
                for (var j = 0; j < room.FillerWords.Count; j++)
                {
                    var fillerWord = room.FillerWords[j];
                    if (fillerWord.index > index)
                        room.FillerWords[j] = (fillerWord.index + words.Length, fillerWord.player);
                }

                //mark them as fillers
                for (var wordIndex = index + 1; wordIndex <= index + words.Length; wordIndex++)
                    room.FillerWords.Add((wordIndex, allFillers[i].player));
            }

            //we wait for the final index of each filler
            foreach (var fillerPLayer in fillerPlayers)
            {
                fillerPLayer.FillersWords = room.FillerWords.Where(f => f.player == fillerPLayer.TurnId)
                    .Select(f => f.index).ToList();

                fillerPLayer.FillersWords.Sort();
            }

            room.Text = string.Join(" ", room.Words);

            room.FillerWords.Sort();
            //will sort based on the index, then on the player, the index must be unique anyway
        }

        private async Task SendPrepareRoom(Room room, List<FullUserInfo> turnSortedUsersInfo,
            List<string> selectedItemPlayers)
        {
            var tasks = new List<Task>();

            var seed = StaticRandom.GetRandom(9999);

            for (var i = 0; i < turnSortedUsersInfo.Count; i++)
            {
                var userInfo = turnSortedUsersInfo[i];
                foreach (var otherUser in turnSortedUsersInfo.Where(u => u != userInfo))
                    otherUser.Friendship = (int)await offlineRepo.GetFriendship(userInfo.Id, otherUser.Id);
                //todo this operation is very expensive

                if (room.RoomActors[i] is not RoomUser ru) continue;

                var chosenPowerUps = room.RoomActors.Select(a => a.ChosenPowerUp).ToList();

                var response = new RoomPrepareResponse
                {
                    TurnSortedUsersInfo = turnSortedUsersInfo,
                    SelectedItemPlayers = selectedItemPlayers,
                    TurnIndex = i,
                    Text = room.Text,
                    FillerWords = room.FillerWords,
                    ChosenPowerUps = chosenPowerUps,
                    Seed = seed,
                };

                var task = masterHub.SendOrderedAsync(ru, "PrepareRequestedRoomRpc", response);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        private RoomUser CreateRoomUser(Room room)
        {
            var roomUser = new RoomUser(scopeRepo.UserId, room);
            scopeRepo.AddNewUser(roomUser);
            return roomUser;
        }

        private async Task StartRoomIfAllReady(Room room)
        {
            var readyUsersCount = room.RoomUsers.Count(u => u.IsReady);
            if (readyUsersCount == room.RoomUsers.Count) //bots don't get ready
            {
                room.IsAllReady = true;
                await gameplay.ReadyGo();
            }
        }

        public void RemovePendingDisconnectedUser(RoomUser roomUser)
        {
            logger.LogInformation("removing pending room user {RoomUserId}", roomUser.Id);

            roomUser.Room.RoomActors.Remove(roomUser);
            roomUser.Room.RoomUsers.Remove(roomUser);
            scopeRepo.RemovePendingUser();

            //maybe the remaining are bots, or a not has only a single user, bots not filled yet
            if (roomUser.Room.RoomUsers.Count == 0)
            {
                scopeRepo.DeleteRoom();
                logger.LogInformation("delete room because it has no users");
            }

            //remove the actor at the end because scope repo depend on it to find everything
            scopeRepo.RemoveRoomUser();
        }
    }
}