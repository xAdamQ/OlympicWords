using System;
using System.Collections.Generic;
using System.Linq;

namespace OlympicWords.Services;

/// <summary>
/// methods are service independent
/// </summary>
public class Room
{
    public bool Started;

    private const int MaxLevel = 999;
    private const float Expo = .55f, Divi = 10;

    public static int GetLevelFromXp(int xp)
    {
        var level = (int)(MathF.Pow(xp, Expo) / Divi);
        return level < MaxLevel ? level : MaxLevel;
    }

    public static int GetStartXpOfLevel(int level)
    {
        if (level == 0) return 0;
        return (int)MathF.Pow(2, MathF.Log2(Divi * level) / Expo);
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

    public int CapacityChoice { get; }
    public int Capacity => Capacities[CapacityChoice];
    public static int[] Capacities => new[] { 2, 3, 4 };

    public IEnumerable<RoomUser> InRoomUsers => RoomUsers.Where(ru => !ru.Cancellation.IsCancellationRequested);

    public bool IsFull => RoomActors.Count == Capacity;

    public void SetUsersDomains(Type domain)
    {
        foreach (var ru in RoomUsers) ru.ActiveUser.Domain = domain;
    }


    //variable based on the room type, keep it simple
    public int wrongDigitProb = -1, botTimeMin = 200, botTimeMax = 700;

    //I may leave this prop public because some feature like nitro may use it
    public string[] Words { get; }
    public string Text { get; }

    public CancellationTokenSource cancellationTokenSource { get; }

    public int FinishedPLayers { get; set; }
    //public int SurrenderPenalty => (int)(.25 * Bet);

    /// <summary>
    /// which is the last sent char to all users including self
    /// can be useful if we care about syncing the buffer with its wrong digits
    /// </summary>
    // public int[] StreamSyncPointer { get; }
    public Room(int category, int capacityChoice, string text)
    {
        CapacityChoice = capacityChoice;
        Text = text;
        Category = category;

        Words = text.Split(' ');
        cancellationTokenSource = new CancellationTokenSource();
    }
}