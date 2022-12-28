using OlympicWords.Common;
using OlympicWords.Services.Extensions;
using Microsoft.AspNetCore.SignalR;
using Common.Lobby;
using OlympicWords.Services.Helpers;

namespace OlympicWords.Services
{
    public interface IMatchMaker
    {
        Task RequestRandomRoom(int category, int capacityChoice, string userId, string connId);

        /// <summary>
        /// called by timeout
        /// </summary>
        Task FillPendingRoomWithBots(Room room);

        Task MakeRoomUserReadyRpc();
        void RemovePendingDisconnectedUser(RoomUser roomUser);

        // Task<MatchRequestResult> RequestMatch(ActiveUser activeUser, string oppoId);
        // void CancelChallengeRequest(ActiveUser activeUser);
        // Task<ChallengeResponseResult> RespondChallengeRequest(ActiveUser activeUser,
        // bool response, string sender);
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

        public async Task RequestRandomRoom(int category, int capacityChoice, string userId, string connId)
        {
            //I set user domain fast because this will await sometime and enable the user to call
            //twice this method without domain change
            scopeRepo.MarkUserPending();

            if (!category.IsInRange(Room.Bets.Length) || !capacityChoice.IsInRange(Room.Capacities.Length))
            {
                scopeRepo.RemovePendingUser();
                throw new Exceptions.BadUserInputException();
            }

            var dUser = await offlineRepo.GetCurrentUserAsync();

            if (dUser.Money < Room.Bets[category])
            {
                scopeRepo.RemovePendingUser();
                throw new Exceptions.BadUserInputException();
            }

            var room = scopeRepo.TakePendingRoom(category, capacityChoice) ?? MakeRoom(category, capacityChoice);
            var roomUser = CreateRoomUser(room, connId);
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

        // public async Task<MatchRequestResult> RequestMatch(ActiveUser activeUser,
        //     string oppoId)
        // {
        //     var dUser = await offlineRepo.GetUserByIdAsyc(activeUser.Id);
        //
        //     if (dUser.Money < Room.MinBet)
        //         throw new Exceptions.BadUserInputException();
        //
        //     //BadUserInputException is thrown when something is wrong but should've been
        //     //validated by the client 
        //
        //     var oppoUser = await offlineRepo.GetUserByIdAsyc(oppoId);
        //     var friendship = await offlineRepo.GetFriendship(activeUser.Id, oppoId);
        //
        //     if (friendship is FriendShip.None or FriendShip.Follower && !oppoUser.EnableOpenMatches)
        //         throw new Exceptions.BadUserInputException();
        //
        //     if (!scopeRepo.IsUserActive(oppoId))
        //         return MatchRequestResult.Offline;
        //
        //     if (scopeRepo.DoesRoomUserExist(oppoId))
        //         return MatchRequestResult.Playing;
        //
        //     if (oppoUser.Money < Room.MinBet)
        //         return MatchRequestResult.NoMoney;
        //
        //     //can't call again because this fun domain is lobby.idle only
        //     activeUser.Domain = typeof(UserDomain.Stateless.Pending);
        //
        //     activeUser.ChallengeRequestTarget = oppoId;
        //
        //     var oppoAu = scopeRepo.GetActiveUser(oppoId);
        //     //oppo is 100% active at this satage
        //
        //     await masterHub.SendOrderedAsync(scopeRepo.GetRoomUser(oppoId), "ChallengeRequest",
        //         Mapper.UserToMinUserInfoFunc(dUser));
        //
        //     return MatchRequestResult.Available;
        // }
        //
        // public void CancelChallengeRequest(ActiveUser activeUser)
        // {
        //     activeUser.ChallengeRequestTarget = null;
        //     activeUser.Domain = typeof(UserDomain.Stateless);
        // }
        //
        // public async Task<ChallengeResponseResult> RespondChallengeRequest(ActiveUser activeUser,
        //     bool response, string sender)
        // {
        //     if (!scopeRepo.IsUserActive(sender))
        //         return ChallengeResponseResult.Offline;
        //
        //     var senderActiveUser = scopeRepo.GetActiveUser(sender);
        //
        //     if (senderActiveUser.ChallengeRequestTarget != activeUser.Id)
        //         //can be null or he sent to another user after
        //         return ChallengeResponseResult.Canceled;
        //
        //     if (!response)
        //     {
        //         await masterHub.SendOrderedAsync(scopeRepo.GetRoomUser(sender),
        //             "RespondChallenge", false);
        //         //otherwise start the room
        //
        //         CancelChallengeRequest(senderActiveUser);
        //
        //         return ChallengeResponseResult.Success;
        //     }
        //
        //     //user domains are changed when prepare is called
        //     senderActiveUser.ChallengeRequestTarget = null;
        //
        //     var room = MakeRoom(0, 0);
        //
        //     var roomUser = CreateRoomUser(activeUser, room);
        //     var senderRoomUser = CreateRoomUser(senderActiveUser, room);
        //
        //     room.RoomUsers.Add(senderRoomUser);
        //     room.RoomActors.Add(senderRoomUser);
        //     room.RoomUsers.Add(roomUser);
        //     room.RoomActors.Add(roomUser);
        //
        //     await PrepareRoom(room);
        //
        //     return ChallengeResponseResult.Success;
        // }

        private Room MakeRoom(int capacityChoice, int category)
        {
            return scopeRepo.AddRoom(new Room(capacityChoice, category));
        }

        public async Task MakeRoomUserReadyRpc()
        {
            scopeRepo.RoomUser.Domain = typeof(UserDomain.Room.WaitingForOthers);
            scopeRepo.RoomUser.IsReady = true;

            await StartRoomIfAllReady(scopeRepo.Room);
        } //doesn't fit into unit testing

        private async Task PrepareRoom(Room room)
        {
            room.SetUsersDomain<UserDomain.Room.GettingReady>();

            room.RoomActors.Shuffle();
            for (var i = 0; i < room.RoomActors.Count; i++) room.RoomActors[i].TurnId = i;

            room.Text = offlineRepo.ChooseText(room.Category);
            room.Words = room.Text.Split(" ").ToList();

            SetFillers(room);

            var userIds = room.RoomActors.Select(ru => ru.Id).ToList();
            var users = await offlineRepo.GetUsersAsync(userIds);

            users.ForEach(u => u.Money -= room.Bet);

            var fullUsersInfos = users.Select(Mapper.UserToFullFunc).ToList();

            var turnSortedUsersInfo = room.RoomActors.Join(fullUsersInfos, actor => actor.Id,
                info => info.Id, (_, info) => info).ToList();

            room.RoomUsers.ForEach(ru => scopeRepo.RemovePendingUser(ru.Id));

            await offlineRepo.SaveChangesAsync();
            serverLoop.SetForceStartRoomTimeout(room);
            await SendPrepareRoom(room, turnSortedUsersInfo);
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
            const int filler = (int)PowerUp.Filler;

            var allFillers = new List<(int player, string fillerText)>();
            var fillerPlayers = room.RoomActors.Where(a => a.ChosenPowerUp == filler).ToList();
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

        private async Task SendPrepareRoom(Room room, List<FullUserInfo> turnSortedUsersInfo)
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

                //actor id = index
                var chosenPowerUps = room.RoomActors.Select(a => a.ChosenPowerUp);

                var task = masterHub.SendOrderedAsync(ru, "PrepareRequestedRoomRpc",
                    turnSortedUsersInfo, i, room.Text, seed, room.FillerWords, chosenPowerUps);

                //changes in the same room when he disconnect
                logger.LogInformation("prepare sent");

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        private RoomUser CreateRoomUser(Room room, string connectionId)
        {
            var roomUser = new RoomUser(scopeRepo.UserId, room, connectionId);
            scopeRepo.AddNewUser(roomUser);
            return roomUser;
        }

        private async Task StartRoomIfAllReady(Room room)
        {
            var readyUsersCount = room.RoomUsers.Count(u => u.IsReady);
            if (readyUsersCount == room.RoomUsers.Count) //bots don't get ready
            {
                serverLoop.CancelForceStart(room);
                await gameplay.StartRoom();
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