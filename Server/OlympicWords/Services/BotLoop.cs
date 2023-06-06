namespace OlympicWords.Services
{
    public interface IServerLoop
    {
        void StartGame(Room room);
    }

    public class BotLoop : IServerLoop
    {
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILogger<BotLoop> logger;

        public BotLoop(IServiceScopeFactory serviceScopeFactory, ILogger<BotLoop> logger)
        {
            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;
        }

        public void StartGame(Room room)
        {
            foreach (var roomBot in room.Bots)
                Start(roomBot, room.CancellationTokenSource.Token);
        }

        /// <summary>
        /// single loop for all bots to advance randomly on fixed update
        /// </summary>
        private void Start(RoomBot roomBot, CancellationToken cancellationToken)
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
                    if (cancellationToken.IsCancellationRequested) break;

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
            }, cancellationToken);
        }
    }
}