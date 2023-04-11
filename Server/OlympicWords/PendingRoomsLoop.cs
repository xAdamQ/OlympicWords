using OlympicWords.Services;

namespace OlympicWords;

public class PendingRoomsLoop : BackgroundService
{
    private readonly TimeSpan updateTIme = TimeSpan.FromMilliseconds(250);

    private readonly ILogger<PendingRoomsLoop> logger;
    private readonly PersistantData persistantData;
    private readonly IServiceScopeFactory serviceScopeFactory;

    private readonly Queue<(Room room, DateTime time)> initiatedRooms;

    public PendingRoomsLoop(ILogger<PendingRoomsLoop> logger, PersistantData persistantData,
        IServiceScopeFactory serviceScopeFactory)
    {
        this.logger = logger;
        this.persistantData = persistantData;
        this.serviceScopeFactory = serviceScopeFactory;

        initiatedRooms = persistantData.InitiatedRooms;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(updateTIme, stoppingToken);

            if (initiatedRooms.Count == 0) continue;

            await HandleDone();
        }
    }

    private async Task HandleDone()
    {
        var now = DateTime.Now;
        while (initiatedRooms.TryPeek(out var r))
        {
            if (now - r.time > TimeSpan.FromMilliseconds(PENDING_ROOM_TIMEOUT))
                await OnPendingRoomTimeout(r.room);
            else if (!r.room.IsFull)
                return;

            initiatedRooms.Dequeue();
        }
    }

    private const int PENDING_ROOM_TIMEOUT = 3 * 1000;
    private async Task OnPendingRoomTimeout(Room room)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var scopeRepo = scope.ServiceProvider.GetService<IScopeRepo>();
        persistantData!.FeedScope(scopeRepo);

        var roomRequester = scope.ServiceProvider.GetService<IMatchMaker>();

        try
        {
            //the errors here shouldn't break the background service
            await roomRequester!.FillPendingRoomWithBots(room);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message, e.StackTrace);
        }
    }
}