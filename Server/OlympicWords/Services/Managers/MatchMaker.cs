using OlympicWords.Common;
using OlympicWords.Services.Extensions;
using Microsoft.AspNetCore.SignalR;
using OlympicWords.Services.Helpers;

namespace OlympicWords.Services
{
    public interface IMatchMaker
    {
        Task RequestRandomRoom(int category, string env);

        /// <summary>
        /// called by timeout
        /// </summary>
        Task FillPendingRoomWithBots(Room room);

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
        private readonly IServerLoop serverLoop;
        private readonly ILogger<MatchMaker> logger;
        private readonly IOfflineRepo offlineRepo;
        private readonly IScopeRepo scopeRepo;

        public MatchMaker(IHubContext<RoomHub> masterHub, IOfflineRepo offlineRepo,
            IScopeRepo scopeRepo, IGameplay gameplay, IServerLoop serverLoop, ILogger<MatchMaker> logger)
        {
            this.masterHub = masterHub;
            this.offlineRepo = offlineRepo;
            this.scopeRepo = scopeRepo;
            this.gameplay = gameplay;
            this.serverLoop = serverLoop;
            this.logger = logger;
        }

        public async Task RequestRandomRoom(int category, string env)
        {
            //I set user domain fast because this will await sometime and enable the user to call
            //twice this method without domain change
            scopeRepo.MarkUserPending();

            if (!category.IsInRange(Room.Bets.Length))
            {
                scopeRepo.RemovePendingUser();
                throw new Exceptions.BadUserInputException($"the room category: {category} exceeds the category list: {Room.Bets.Length}");
            }

            if(!OfflineRepo.GameConfig.OrderedEnvs.Contains(env))
            {
                scopeRepo.RemovePendingUser();
                throw new Exceptions.BadUserInputException($"the environment {env} doesn't exist");
            }

            var dUser = await offlineRepo.GetCurrentUserAsync();

            if (dUser.Money < Room.Bets[category])
            {
                scopeRepo.RemovePendingUser();
                throw new Exceptions.BadUserInputException();
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
                serverLoop.CancelPendingRoomTimeout(room);
                await PrepareRoom(room);
            }
            else
            {
                scopeRepo.KeepPendingRoom(room); //so other users can see it
                serverLoop.SetupPendingRoomTimeoutIfNotExist(room);

                // const int timeout = 1000;
                // const int checkInterval = 250;
                // const int maxRetries = timeout / checkInterval;
                //
                // var retries = 0;
                //
                // while (!room.IsFull && retries < maxRetries)
                // {
                //     await Task.Delay(checkInterval * 1000);
                //     retries++;
                // }
                //
                // if (room.IsFull)
                //     await PrepareRoom(room);
                // else
                //     await FillPendingRoomWithBots(room);
            }
        }

        /// <summary>
        /// called by timeout
        /// </summary>
        public async Task FillPendingRoomWithBots(Room room)
        {
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

            var usersInfos = turnSortedUsers.Select(Mapper.UserToFullFunc).ToList();
            var selectedItemPlayers = turnSortedUsers.Select(u => u.SelectedItemPlayer[room.Env]).ToList();

            room.RoomUsers.ForEach(ru => scopeRepo.RemovePendingUser(ru.Id));

            await offlineRepo.SaveChangesAsync();
            serverLoop.SetForceStartRoomTimeout(room);
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
                    Seed = seed
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
                serverLoop.CancelForceStart(room);
                await gameplay.ReadyGo();
            }
        }

        public void RemovePendingDisconnectedUser(RoomUser roomUser)
        {
            logger.LogInformation("removing pending room user {RoomUserId}", roomUser.Id);

            roomUser.Room.RoomActors.Remove(roomUser);
            roomUser.Room.RoomUsers.Remove(roomUser);
            scopeRepo.RemovePendingUser();

            if (roomUser.Room.RoomUsers.Count == 0) //maybe the remaining are bots, or non
            {
                // serverLoop.CancelPendingRoomTimeout(roomUser.Room);
                scopeRepo.DeleteRoom();
            }

            scopeRepo.RemoveRoomUser();
        }
    }
}