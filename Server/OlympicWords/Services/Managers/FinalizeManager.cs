using Basra.Common;
using OlympicWords.Services.Extensions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using OlympicWords.Services.Helpers;

namespace OlympicWords.Services
{
    public interface IFinalizeManager
    {
        Task FinalizeRoom(Room room);
    }

    public class FinalizeManager : IFinalizeManager
    {
        private readonly IHubContext<MasterHub> masterHub;
        private readonly IOfflineRepo offlineRepo;
        private readonly IOnlineRepo onlineRepo;
        private readonly ILogger<FinalizeManager> logger;
        public FinalizeManager(IHubContext<MasterHub> masterHub, IOfflineRepo offlineRepo,
            IOnlineRepo onlineRepo,
            ILogger<FinalizeManager> logger)
        {
            this.masterHub = masterHub;
            this.offlineRepo = offlineRepo;
            this.onlineRepo = onlineRepo;
            this.logger = logger;
        }

        public async Task FinalizeRoom(Room room)
        {
            room.SetUsersDomains(typeof(UserDomain.App.Room.FinishedRoom));
            //does this has usage or u can just use lobby idle? 

            LastEat(room);

            var roomDataUsersUnOrdered =
                await offlineRepo.GetUsersByIdsAsync(room.RoomActors.Select(_ => _.Id).ToList());

            var roomDataUsers = room.RoomActors.Join(roomDataUsersUnOrdered, ru => ru.Id,
                du => du.Id, (_, du) => du).ToList();

            var scores = CalcScores(room.RoomActors);

            var reportsAndStatus = UpdateUserStates(room, roomDataUsers, scores);

            for (var i = 0; i < roomDataUsers.Count; i++)
            {
                ActiveUser activeUser = room.RoomActors[i] is RoomUser ru
                ? activeUser = ru.ActiveUser
                : null;

                await LevelWorks(roomDataUsers[i], activeUser);
            }

            await offlineRepo.SaveChangesAsync();

            await SendFinalizeResult(room.RoomUsers, roomDataUsers, reportsAndStatus.Item1,
                reportsAndStatus.Item2, room.LastEater.TurnId);

            RemoveDisconnectedUsers(room.RoomUsers);

            room.RoomUsers.ForEach(ru => onlineRepo.DeleteRoomUser(ru));
            onlineRepo.DeleteRoom(room);

            room.SetUsersDomains(typeof(UserDomain.App.Lobby.Idle));
        }

        private List<int> CalcScores(List<RoomActor> roomActors)
        {
            var biggestEatenCount = roomActors.Max(u => u.EatenCardsCount);
            var biggestEaters = roomActors.Where(u => u.EatenCardsCount == biggestEatenCount)
                .ToArray();

            var scores = new List<int>();

            foreach (var roomActor in roomActors)
            {
                scores.Add(roomActor.BasraCount * 10 +
                           roomActor.BigBasraCount * 30 +
                           (biggestEaters.Contains(roomActor) ? 30 : 0));
            }

            return scores;
        }

        /// <returns> the added xp and for what </returns>
        private (List<RoomXpReport>, List<UserRoomStatus>) UpdateUserStates(Room room,
            List<User> dataUsers, List<int> scores)
        {
            var xpReports = new List<RoomXpReport>();
            for (var i = 0; i < room.Capacity; i++) xpReports.Add(new RoomXpReport());
            var userRoomStatus = room.RoomActors.Select(u => new UserRoomStatus
            {
                Basras = u.BasraCount,
                BigBasras = u.BigBasraCount,
                EatenCards = u.EatenCardsCount,
            }).ToList();

            var betWithoutTicket = (int)(room.Bet / 1.1f);
            var totalBet = betWithoutTicket * room.Capacity;
            var maxScore = scores.Max();
            var betXp = CalcBetXp(room.Category);

            var winnerIndices = scores.Select((score, i) => score == maxScore ? i : -1)
                .Where(scoreIndex => scoreIndex != -1)
                .ToList();
            var loserIndices = Enumerable.Range(0, room.Capacity)
                .Where(i => !winnerIndices.Contains(i))
                .ToList();

            //drawers
            if (winnerIndices.Count > 1)
            {
                var moneyPart = totalBet / winnerIndices.Count;
                foreach (var userIndex in winnerIndices)
                {
                    var dUser = dataUsers[userIndex];
                    dUser.Draws++;
                    dUser.Money += userRoomStatus[userIndex].WinMoney = moneyPart;
                    dUser.TotalEarnedMoney += moneyPart;

                    xpReports[userIndex].Competition = (int)(Room.DrawXpPercent * betXp);
                }
            }
            //winner
            else
            {
                var dUser = dataUsers[winnerIndices[0]];
                dUser.WonRoomsCount++;
                dUser.Money += userRoomStatus[winnerIndices[0]].WinMoney = totalBet;
                dUser.TotalEarnedMoney += totalBet;
                dUser.WinStreak++;

                xpReports[0].Competition = (int)(Room.WinXpPercent * betXp);
            }


            //losers
            foreach (var loserIndex in loserIndices)
            {
                var dUser = dataUsers[loserIndex];
                xpReports[loserIndex].Competition = (int)(Room.LoseXpPercent * betXp);

                dUser.WinStreak = 0;
            }

            for (var i = 0; i < room.Capacity; i++)
            {
                var dUser = dataUsers[i];
                var roomActor = room.RoomActors[i];

                dUser.PlayedRoomsCount++;

                dUser.EatenCardsCount += roomActor.EatenCardsCount;
                dUser.BasraCount += roomActor.BasraCount;
                dUser.BigBasraCount += roomActor.BigBasraCount;

                dUser.Xp = xpReports[i].Competition;

                xpReports[i].Basra = roomActor.BasraCount * (int)(Room.BasraXpPercent * betXp);
                dUser.Xp += xpReports[i].Basra;

                xpReports[i].BigBasra =
                    roomActor.BigBasraCount * (int)(Room.BigBasraXpPercent * betXp);
                dUser.Xp += xpReports[i].BigBasra;

                if (roomActor.EatenCardsCount > Room.GreatEatThreshold)
                {
                    xpReports[i].GreatEat = (int)(Room.GreatEatXpPercent * betXp);
                    dUser.Xp += xpReports[i].GreatEat;
                }

                if (dUser.MaxWinStreak < dUser.WinStreak)
                    dUser.MaxWinStreak = dUser.WinStreak;
            }

            return (xpReports, userRoomStatus);
        }

        private int CalcBetXp(int betChoice) => (int)(100 * MathF.Pow(betChoice, 1.4f)) + 100;

        private void LastEat(Room room)
        {
            room.LastEater.EatenCardsCount += room.GroundCards.Count;

            room.GroundCards.Clear();
        }

        /// <summary>
        /// check current level against xp to level up and send to client
        /// functions that takes data user as param doesn't save changes
        /// </summary>
        private async Task LevelWorks(User roomDataUser, ActiveUser activeUser)
        {
            var calcedLevel = Room.GetLevelFromXp(roomDataUser.Xp);
            if (calcedLevel > roomDataUser.Level)
            {
                var increasedLevels = calcedLevel - roomDataUser.Level;
                var totalMoneyReward = 0;
                for (var j = 0; j < increasedLevels; j++)
                {
                    totalMoneyReward += 100;
                    //todo give level up rewards (money equation), add to test
                    //todo test this function logic
                }

                roomDataUser.Level = calcedLevel;
                roomDataUser.Money += totalMoneyReward;

                if (activeUser != null)
                    await masterHub.SendOrderedAsync(activeUser, "LevelUp", calcedLevel, totalMoneyReward);
            }
        } //separate this to be called on every XP change 

        private async Task SendFinalizeResult(List<RoomUser> roomUsers, List<User> roomDataUsers,
            List<RoomXpReport> roomXpReports, List<UserRoomStatus> userRoomStatuses,
            int lastEaterTurnId)
        {
            // var xpRepMapped = roomUsers.Join(roomXpReports, rUser => rUser.TurnId,
            // roomXpReports.IndexOf, (_, report) => report).ToList();

            var finalizeTasks = new List<Task>();
            for (var i = 0; i < roomUsers.Count; i++)
            {
                var finalizeResult = new FinalizeResult
                {
                    RoomXpReport = roomXpReports[i],
                    PersonalFullUserInfo = Mapper.ConvertUserDataToClient(roomDataUsers[i]),
                    LastEaterTurnId = lastEaterTurnId,
                    UserRoomStatus = userRoomStatuses,
                };

                finalizeTasks.Add(masterHub.SendOrderedAsync(roomUsers[i].ActiveUser,
                    "FinalizeRoom", finalizeResult));
            }

            logger.LogInformation("finalize called");

            await Task.WhenAll(finalizeTasks);
        }

        private void RemoveDisconnectedUsers(List<RoomUser> roomUsers)
        {
            foreach (var roomUser in roomUsers.Where(ru => ru.ActiveUser.IsDisconnected))
                //where filtered with new collection, I don't know the performance but I will see how
                //linq works under the hood, because I think the created collection doesn't affect performance
                onlineRepo.RemoveActiveUser(roomUser.ActiveUser.Id);
        }
    }
}