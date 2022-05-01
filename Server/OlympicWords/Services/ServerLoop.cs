using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace OlympicWords.Services
{
    public interface IServerLoop
    {
        void SetupTurnTimeout(RoomUser roomUser);
        void CancelTurnTimeout(RoomUser roomUser);
        /// <summary>
        /// when players a late for ready
        /// </summary>
        void SetForceStartRoomTimeout(Room room);
        void CancelForceStart(Room room);
        void SetupPendingRoomTimeoutIfNotExist(Room room);
        void CancelPendingRoomTimeout(Room room);
        void BotPlay(RoomBot roomBot);
        // void CancelTurnTimeoutIfExist(RoomUser roomUser);

        // void SetFriendlyRequestTimeout(string sender);
        // void CancelFriendlyRequestTimeout((string, string) senderTarget);
    }

    public class ServerLoop : IServerLoop
    {
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly IOnlineRepo onlineRepo;

        public ServerLoop(IServiceScopeFactory serviceScopeFactory, IOnlineRepo onlineRepo)
        {
            this.serviceScopeFactory = serviceScopeFactory;
            this.onlineRepo = onlineRepo;
        }

        //for users who miss turn
        private Dictionary<RoomUser, CancellationTokenSource> TurnCancellations { get; } = new();
        private const int TurnTime = 10 * 1000; //todo take config from dev and prod json
        public void SetupTurnTimeout(RoomUser roomUser)
        {
            var cSource = new CancellationTokenSource();
            TurnCancellations.Add(roomUser, cSource);

            Task.Delay(TurnTime, cSource.Token).ContinueWith(task =>
            {
                if (!task.IsCanceled) Task.Run(() => OnTurnTimeout(roomUser));
            });
        }
        private async Task OnTurnTimeout(RoomUser roomUser)
        {
            TurnCancellations.Remove(roomUser);

            using (var scope = serviceScopeFactory.CreateScope())
            {
                var roomUserManager = scope.ServiceProvider.GetService<IRoomManager>();
                await roomUserManager!.ForceUserPlay(roomUser);
            }
        }
        public void CancelTurnTimeout(RoomUser roomUser)
        {
            TurnCancellations[roomUser].Cancel();
            TurnCancellations.Remove(roomUser);
        }

        private Dictionary<Room, CancellationTokenSource> PendingRoomCancellations { get; } = new();
        private const int PendingRoomTimeout = 8 * 1000;
        public void SetupPendingRoomTimeoutIfNotExist(Room room)
        {
            if (PendingRoomCancellations.ContainsKey(room)) return;

            var cSource = new CancellationTokenSource();
            PendingRoomCancellations.Add(room, cSource);

            Task.Delay(PendingRoomTimeout, cSource.Token).ContinueWith(task =>
            {
                if (!task.IsCanceled) Task.Run(() => OnPendingRoomTimeout(room));
            });
        }
        private async Task OnPendingRoomTimeout(Room room)
        {
            PendingRoomCancellations.Remove(room);

            using (var scope = serviceScopeFactory.CreateScope())
            {
                var roomRequester = scope.ServiceProvider.GetService<IMatchMaker>();
                await roomRequester!.FillPendingRoomWithBots(room);
            }
        }
        public void CancelPendingRoomTimeout(Room room)
        {
            PendingRoomCancellations[room].Cancel();
            PendingRoomCancellations.Remove(room);
        }

        private Dictionary<Room, CancellationTokenSource> ForceStartCancellations { get; } = new();
        private const int ReadyTimeout = 8 * 1000;
        public void SetForceStartRoomTimeout(Room room)
        {
            var cSource = new CancellationTokenSource();
            ForceStartCancellations.Add(room, cSource);

            Task.Delay(ReadyTimeout, cSource.Token)
                .ContinueWith(task =>
                {
                    if (!task.IsCanceled) Task.Run(() => OnForceStartTimeout(room));
                });
        }
        private async Task OnForceStartTimeout(Room room)
        {
            ForceStartCancellations.Remove(room);

            using (var scope = serviceScopeFactory.CreateScope())
            {
                //this will fail because you don't have scope
                var roomManager = scope.ServiceProvider.GetService<IRoomManager>();
                await roomManager!.StartRoom(room);
            }
        }
        public void CancelForceStart(Room room)
        {
            ForceStartCancellations[room].Cancel();
            ForceStartCancellations.Remove(room);
        }

        #region friendly req

        // private Dictionary<(string, string), CancellationTokenSource>
        //     FriendlyMatchCancellations { get; } = new();
        // private List<Action> CancelTimoutDelegates;
        // private const int FriendlyRequestTimeout = 30 * 1000;
        // public void SetFriendlyRequestTimeout((string, string) senderTarget)
        // {
        //     var cSource = new CancellationTokenSource();
        //     FriendlyMatchCancellations.Add(senderTarget, cSource);
        //
        //     Task.Delay(FriendlyRequestTimeout, cSource.Token)
        //         .ContinueWith(task =>
        //         {
        //             if (!task.IsCanceled)
        //                 Task.Run(() => OnFriendlyMatchTimeout(senderTarget.Item1));
        //         });
        //
        //     sessionRepo.GetActiveUser(senderTarget.Item1).Disconnected +=
        //         () => CancelFriendlyRequestTimeout(senderTarget);
        // }
        // private async Task OnFriendlyMatchTimeout(string sender)
        // {
        //     FriendlyMatchCancellations.Remove(sender);
        //
        //     if (sessionRepo.IsUserActive(sender))
        //     {
        //         using (var scope = _serviceScopeFactory.CreateScope())
        //         {
        //             sessionRepo.GetActiveUser(sender).Domain = typeof(UserDomain.App.Lobby);
        //
        //             var roomManager = scope.ServiceProvider.GetService<IHubContext<MasterHub>>();
        //             await roomManager!.Clients.User(sender).SendAsync("CancelFriendlyRequest");
        //         }
        //     }
        // }
        // public void CancelFriendlyRequestTimeout((string, string) senderTarget)
        // {
        //     if (sessionRepo.GetActiveUser(senderTarget.Item1) != null)
        //         sessionRepo.GetActiveUser(senderTarget.Item1).Disconnected -=
        //             () => CancelFriendlyRequestTimeout(senderTarget);
        //
        //     FriendlyMatchCancellations[sender].Cancel();
        //     FriendlyMatchCancellations.Remove(sender);
        // }
        // public bool DoesRequestExist((string, string) senderTarget)
        // {
        //     return FriendlyMatchCancellations.ContainsKey(senderTarget);
        // }

        #endregion

        private const int BotPlayMin = 500, BotPlayMax = 4000;

        public void BotPlay(RoomBot roomBot)
        {
            Task.Run(async () =>
            {
                await Task.Delay(StaticRandom.GetRandom(BotPlayMin, BotPlayMax));

                using var scope = serviceScopeFactory.CreateScope();
                var roomManager = scope.ServiceProvider.GetService<IRoomManager>();
                await roomManager!.BotPlay(roomBot);
            }).ContinueWith(t =>
            {
                if (t.Exception != null) throw t.Exception;
            });
        }


        // private Dictionary<Action, CancellationTokenSource> DelayCancellations { get; } = new();
        // public void Delay(int delay, Action onComplete, Object key)
        // {
        //     var cSource = new CancellationTokenSource();
        //     DelayCancellations.Add(onComplete, cSource);
        //
        //     Task.Delay(ReadyTimeout, cSource.Token).ContinueWith(_ => onComplete, cSource.Token);
        //     DelayCancellations.Remove(onComplete);
        // }
        // public void CancelDelay(Room room)
        // {
        //     ForceStartCancellations[room].Cancel();
        // }


        // public void StartTurn(string userId)
        // {
        //     var turnTime = 10000;
        //     var cSource = new CancellationTokenSource();
        //     TurnCancellations.Add(userId, cSource);
        //
        //     Task.Delay(turnTime).ContinueWith(t => OnTurnTimout(userId), cSource.Token);
        // }
        //
        // private async Task OnTurnTimout(string userId)
        // {
        //     // await RandomPlay(userId);
        // }

        // private async Task RandomPlay(string userId)
        // {
        //     //pick user
        //     //
        //
        //     var randomCardIndex = StaticRandom.GetRandom(Cards.Count);
        //
        //     await Task.WhenAll
        //     (
        //         Play(randomCardIndex),
        //         Program.HubContext.Clients.Client(ConnectionId).SendAsync("OverrideMyLastThrow", randomCardIndex)
        //         // Structure.SendAsync("OverrideThrow", card)
        //     );
        // }

        // protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        // {
        //     Console.WriteLine("excute,,,,,,");
        // }
    }
}