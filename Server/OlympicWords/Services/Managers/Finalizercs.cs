using OlympicWords.Common;
using OlympicWords.Services.Extensions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using OlympicWords.Data;
using OlympicWords.Services.Helpers;

namespace OlympicWords.Services
{
    public interface IFinalizer
    {
        Task Surrender();
        Task FinalizeUser();
    }

    public class Finalizer : IFinalizer
    {
        private readonly IHubContext<RoomHub> masterHub;
        private readonly IOfflineRepo offlineRepo;
        private readonly IScopeRepo scopeRepo;
        private readonly ILogger<Finalizer> logger;

        public Finalizer(IHubContext<RoomHub> masterHub, IOfflineRepo offlineRepo,
            IScopeRepo scopeRepo, ILogger<Finalizer> logger)
        {
            this.masterHub = masterHub;
            this.offlineRepo = offlineRepo;
            this.scopeRepo = scopeRepo;
            this.logger = logger;
        }

        public async Task Surrender()
        {
            //the room user is removed when you disconnect automatically

            scopeRepo.RoomUser.Domain = typeof(UserDomain.Stateless);

            var room = scopeRepo.Room;
            var roomUser = scopeRepo.RoomUser;
            var dataUser = await offlineRepo.GetCurrentUserAsync();

            //dataUser.Money -= room.SurrenderPenalty;
            //bet money is enough penalty for preventing enter and leave misuse

            dataUser.PlayedRoomsCount++;
            await offlineRepo.SaveChangesAsync();

            foreach (var ru in room.InRoomUsers.Where(ru => ru != roomUser))
                await masterHub.SendOrderedAsync(ru, "UserSurrender", roomUser.Id);

            masterHub.Clients.Client(roomUser.ConnectionId);
        }

        public async Task FinalizeUser()
        {
            logger.LogInformation("finalize room");
            var room = scopeRepo.Room;
            var roomActor = scopeRepo.RoomActor;
            var realUser = roomActor as RoomUser;
            var dataUser = await offlineRepo.GetCurrentUserAsync();

            if (realUser is not null)
                realUser.Domain = typeof(UserDomain.Room.Finished);

            roomActor.EndTime = DateTime.Now;

            UserRoomStatus userRoomStatus = new();
            userRoomStatus.FinalPosition = room.FinishedPLayers++;
            userRoomStatus.EarnedMoney = (int)GetEarnedMoney(userRoomStatus.FinalPosition);
            var finishInterval = (float)(roomActor.EndTime - roomActor.StartTime).TotalMinutes;
            userRoomStatus.Wpm = room.Words.Count / finishInterval;
            userRoomStatus.Score = (int)(room.CategoryScoreMultiplier * userRoomStatus.Wpm);

            dataUser.Money += userRoomStatus.EarnedMoney;
            dataUser.Xp += userRoomStatus.Score;
            dataUser.TotalEarnedMoney += userRoomStatus.EarnedMoney;
            dataUser.PlayedRoomsCount++;
            //todo add long term statistics like top 3 positions count, max and average wpm(no need for graph)

            await LevelUp(dataUser);

            await offlineRepo.SaveChangesAsync();

            if (realUser is not null)
                await masterHub.SendOrderedAsync(scopeRepo.RoomUser, "FinalizeRoom", userRoomStatus);

            foreach (var oppo in room.InRoomUsers.Where(ru => ru != roomActor))
                await masterHub.SendOrderedAsync(oppo, "TakeOppoUserRoomStatus", roomActor.TurnId, userRoomStatus);

            if (room.RoomUsers.All(ru => ru.Cancellation.IsCancellationRequested))
                scopeRepo.DeleteRoom();
        }

        float GetEarnedMoney(int finalPosition)
        {
            var room = scopeRepo.Room;
            switch (finalPosition)
            {
                case 0:
                    return room.TotalBet * .7f;
                case 1:
                    return room.TotalBet * .2f;
                case 2:
                    return room.TotalBet * .1f;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// check current level against xp to level up and send to client
        /// functions that takes data user as param doesn't save changes
        /// </summary>
        private async Task LevelUp(User roomDataUser)
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

                if (scopeRepo.IsRealOwner)
                    await masterHub.SendOrderedAsync(scopeRepo.RoomUser, "LevelUp", calcedLevel,
                        totalMoneyReward);
            }
        } //separate this to be called on every XP change 
    }
}