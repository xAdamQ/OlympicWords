namespace OlympicWords.Services;

/// <summary>
/// methods are service independent
/// </summary>
public class Room
{
    private const int MAX_LEVEL = 999;
    private const float EXPO = .55f, DIVIDER = 10;

    public static int GetLevelFromXp(int xp)
    {
        var level = (int)(MathF.Pow(xp, EXPO) / DIVIDER);
        return level < MAX_LEVEL ? level : MAX_LEVEL;
    }

    public static int GetStartXpOfLevel(int level)
    {
        if (level == 0) return 0;
        return (int)MathF.Pow(2, MathF.Log2(DIVIDER * level) / EXPO);
    }

    //don't bother yourself because it's readonly because if converted to database
    //the list will be handled differently
    public List<RoomUser> RoomUsers { get; } = new();
    public List<RoomActor> RoomActors { get; } = new();
    public List<RoomBot> Bots { get; } = new(); //left null on purpose

    public List<string> RoomActorIds => RoomActors.Select(ra => ra.Id).ToList();

    public int Id { get; set; }

    public int Category { get; }
    public int Bet => Bets[Category];
    public static int[] Bets => new[] { 55, 110, 220, 550, 1100, 5500 };
    public int TotalBet => Bet * Capacity;

    /// <summary>
    /// used in money aim code
    /// </summary>
    public static int MinBet => Bets[0];

    public static float[] CategoryScoreMultipliers = { .5f, 1f, 1.5f, 2, 2.5f, 3f };
    public float CategoryScoreMultiplier => CategoryScoreMultipliers[Category];

    public string Env { get; }
    public IEnumerable<RoomUser> InRoomUsers => RoomUsers.Where(ru => !ru.Cancellation.IsCancellationRequested);

    public int Capacity { get; } = 4;
    public bool IsFull => RoomActors.Count == Capacity;
    public bool IsStarted, IsAllReady, IsDeleted;

    public void SetUsersDomain<T>() where T : UserDomain.Room
    {
        var domain = typeof(T);
        foreach (var ru in RoomUsers) ru.Domain = domain;
    }

    //will be decided at run time, based on player average speed
    public const int WRONG_CHAR_PROB = -1;

    //I may leave this prop public because some feature like nitro may use it
    public List<string> Words { get; set; }
    public string Text { get; set; }
    public List<(int index, int player)> FillerWords { get; } = new();

    public CancellationTokenSource CancellationTokenSource { get; }

    public int FinishedPLayers { get; set; }
    //public int SurrenderPenalty => (int)(.25 * Bet);

    public async Task Start(IOfflineRepo offlineRepo)
    {
        IsStarted = true;

        var watch = System.Diagnostics.Stopwatch.StartNew();

        //these users should be cached anyway
        var users = await offlineRepo.GetUsersAsync(RoomUsers.Select(ru => ru.Id).ToList());

        watch.Stop();
        Console.WriteLine("room users get time: " + watch.ElapsedMilliseconds);

        var roomWpm = users.Sum(ru => ru.AverageWpm) / users.Count;
        Bots.ForEach(rb => rb.SetSpeed(roomWpm));

        foreach (var ru in RoomActors)
            ru.StartTime = DateTime.Now;
        // foreach (var ru in RoomUsers)
        // await hub.Groups.AddToGroupAsync(ru.ActiveUser.ConnectionId, GroupName);
        //this grouping stuff shouldn't be used because I use the message id to preserve messages order with a queue on the client
        //here is a big catch, what if the user dot disconnected and the connection id changes after reconnecting
        //this means that my old approach of using the user id regardless of the connection id is better
        //because it is consistent

        //however, azure struggles with routing, that's why groups are made, and it becomes a bottleneck at some point
    }

    /// <summary>
    /// which is the last sent char to all users including self
    /// can be useful if we care about syncing the buffer with its wrong digits
    /// </summary>
    // public int[] StreamSyncPointer { get; }
    public Room(int category, string env)
    {
        Env = env;
        Category = category;

        CancellationTokenSource = new CancellationTokenSource();
    }
}