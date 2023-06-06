using OlympicWords.Services;

namespace OlympicWords;

public class PendingRoomsLoop : BackgroundService
{
    private readonly TimeSpan updateTime = TimeSpan.FromMilliseconds(250);
    private static DateTime Now => DateTime.Now;

    private readonly ILogger<PendingRoomsLoop> logger;
    private readonly PersistantData persistantData;
    private readonly IServiceScopeFactory serviceScopeFactory;

    private readonly Queue<(Room room, DateTime time)> initiatedRooms, unreadyRooms;

    public PendingRoomsLoop(ILogger<PendingRoomsLoop> logger, PersistantData persistantData,
        IServiceScopeFactory serviceScopeFactory)
    {
        this.logger = logger;
        this.persistantData = persistantData;
        this.serviceScopeFactory = serviceScopeFactory;

        initiatedRooms = persistantData.InitiatedRooms;
        unreadyRooms = persistantData.UnreadyRooms;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(updateTime, stoppingToken);

            if (initiatedRooms.Count == 0) continue;

            await BotFillOrStart();
            await TryForceStart();
        }
    }

    private static readonly TimeSpan pendingRoomTimeout = TimeSpan.FromSeconds(8);
    private async Task BotFillOrStart()
    {
        while (initiatedRooms.TryPeek(out var r))
        {
            if (Now - r.time > pendingRoomTimeout)
                await onPendingRoomTimeout(r.room);
            else if (!r.room.IsFull)
                return;

            initiatedRooms.Dequeue();
        }

        async Task onPendingRoomTimeout(Room room)
        {
            if (room.IsDeleted)
            {
                logger.LogInformation("Room {RoomId} was deleted, cancel filling it with bots", room.Id);
                return;
            }

            using var scope = serviceScopeFactory.CreateScope();
            var scopeRepo = scope.ServiceProvider.GetService<IScopeRepo>();
            var roomRequester = scope.ServiceProvider.GetService<IMatchMaker>();

            persistantData!.FeedScope(scopeRepo);
            scopeRepo!.SetRoom(room);

            try
            {
                //the errors here shouldn't break the background service
                await roomRequester!.FillPendingRoomWithBots();
            }
            catch (Exception e)
            {
                logger.LogError("{Exc} \n {Stack}", e.Message, e.StackTrace);
            }
        }
    }

    private static readonly TimeSpan readyTimeout = TimeSpan.FromSeconds(8);
    private async Task TryForceStart()
    {
        while (unreadyRooms.TryPeek(out var r))
        {
            if (Now - r.time > readyTimeout)
            {
                if (!r.room.IsAllReady)
                    await onForceStartTimeout(r.room);

                unreadyRooms.Dequeue();
            }
        }

        async Task onForceStartTimeout(Room room)
        {
            if (room.IsDeleted) return;

            using var scope = serviceScopeFactory.CreateScope();
            var roomManager = scope.ServiceProvider.GetService<IGameplay>();
            var scopeRepo = scope.ServiceProvider.GetService<IScopeRepo>();

            persistantData!.FeedScope(scopeRepo);
            scopeRepo!.SetRoom(room);

            try
            {
                await roomManager!.ReadyGo();
            }
            catch (Exception e)
            {
                logger.LogError("{Exc} \n {Stack}", e.Message, e.StackTrace);
            }
        }
    }
}