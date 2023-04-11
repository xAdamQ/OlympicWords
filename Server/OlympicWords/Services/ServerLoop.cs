namespace OlympicWords.Services
{
    public interface IServerLoop
    {
        /// <summary>
        /// when players a late for ready
        /// </summary>
        void SetForceStartRoomTimeout(Room room);
        void CancelForceStart(Room room);
        void StartGame(Room room);
    }

    public class ServerLoop : IServerLoop
    {
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILogger<ServerLoop> logger;

        public ServerLoop(IServiceScopeFactory serviceScopeFactory, ILogger<ServerLoop> logger)
        {
            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;
        }

        #region ready timoue
        private Dictionary<Room, CancellationTokenSource> ForceStartCancellations { get; } = new();
        private const int READY_TIMEOUT = 8 * 1000;
        public void SetForceStartRoomTimeout(Room room)
        {
            var cSource = new CancellationTokenSource();
            ForceStartCancellations.Add(room, cSource);

            Task.Delay(READY_TIMEOUT, cSource.Token)
                .ContinueWith(async t => await OnForceStartTimeout(room), cSource.Token);
        }
        private async Task OnForceStartTimeout(Room room)
        {
            ForceStartCancellations.Remove(room);

            using var scope = serviceScopeFactory.CreateScope();
            //this will fail because you don't have scope
            var roomManager = scope.ServiceProvider.GetService<IGameplay>();
            await roomManager!.ReadyGo();
        }
        public void CancelForceStart(Room room)
        {
            ForceStartCancellations[room].Cancel();
            ForceStartCancellations.Remove(room);
        }
        #endregion

        public void StartGame(Room room)
        {
            foreach (var roomBot in room.Bots)
                BotLoop(roomBot, room.CancellationTokenSource.Token);
        }

        /// <summary>
        /// single loop for all bots to advance randomly on fixed update
        /// </summary>
        public void BotLoop(RoomBot roomBot, CancellationToken cancellationToken)
        {
            const string ALL_CHARS = "$%#@!*abcdefghijklmnopqrstuvwxyz1234567890?;:ABCDEFGHIJKLMNOPQRSTUVWXYZ^&";

            Task.Factory.StartNew(async () =>
            {
                var room = roomBot.Room;

                using var scope = serviceScopeFactory.CreateScope();
                scope.ServiceProvider.GetService<IScopeRepo>()!.SetBotOwner(roomBot);

                var gameplay = scope.ServiceProvider.GetService<IGameplay>()!;
                var scopeRepo = scope.ServiceProvider.GetService<IScopeRepo>()!;
                var persistantData = scope.ServiceProvider.GetService<PersistantData>()!;
                persistantData.FeedScope(scopeRepo);

                while (roomBot.TextPointer < room.Text.Length)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    char chr;

                    var canJump = (roomBot.ChosenPowerUp == 0 && roomBot.UsedJets < 2) ||
                                  (roomBot.ChosenPowerUp == 1 && roomBot.UsedJets < 1);

                    if (canJump && StaticRandom.GetRandom(10) == 5)
                        chr = '\r';
                    else
                        chr = StaticRandom.GetRandom(100) > Room.WRONG_CHAR_PROB
                            ? room.Text[roomBot.TextPointer]
                            : ALL_CHARS[StaticRandom.GetRandom(ALL_CHARS.Length)];

                    await gameplay.ProcessChar(chr);

                    await Task.Delay(StaticRandom.GetRandom(roomBot.BotTimeMin, roomBot.BotTimeMax), cancellationToken);
                }
            });
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
    }
}