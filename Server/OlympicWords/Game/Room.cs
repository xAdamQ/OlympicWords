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

    public const int DeckSize = 52; //todo make it change with user count
    public const int ShapeSize = 13;

    //each bet has specific xp gain, when you lose for example you take only .25 only of it 
    public const float
        LoseXpPercent = .25f,
        DrawXpPercent = .5f,
        WinXpPercent = 1f,
        BasraXpPercent = .05f,
        BigBasraXpPercent = 1f,
        GreatEatXpPercent = .3f;

    public const int GreatEatThreshold = 38;

    public RoomActor LastEater { get; set; }

    private const int MaxLevel = 999;
    private const float Expo = .55f, Divi = 10;

    public static int GetLevelFromXp(int xp)
    {
        var level = (int) (MathF.Pow(xp, Expo) / Divi);
        return level < MaxLevel ? level : MaxLevel;
    }

    public static int GetStartXpOfLevel(int level)
    {
        if (level == 0) return 0;
        return (int) MathF.Pow(2, MathF.Log2(Divi * level) / Expo);
    }

    public List<int> GroundCards { get; set; }

    //don't bother yourself because it's readonly because if converted to database
    //the list will be handled differently
    public List<RoomUser> RoomUsers { get; } = new();
    public List<RoomActor> RoomActors { get; } = new();
    public List<RoomBot> RoomBots { set; get; } //left null on purpose

    public List<string> RoomActorIds => RoomActors.Select(ra => ra.Id).ToList();

    public int Id { get; set; }

    public int Category { get; }
    public int Bet => Bets[Category];
    public static int[] Bets => new[] {55, 110, 220, 550, 1100, 5500};

    /// <summary>
    /// used in money aim code
    /// </summary>
    public static int MinBet => Bets[0];

    public int CapacityChoice { get; }
    public int Capacity => Capacities[CapacityChoice];
    public static int[] Capacities => new[] {2, 3, 4};

    public List<int> Deck { get; set; }
    public int CurrentTurn { get; set; }

    public bool IsFull => RoomActors.Count == Capacity;

    public void SetUsersDomains(Type domain)
    {
        foreach (var ru in RoomUsers) ru.ActiveUser.Domain = domain;
    }

    public List<char>[] CharBuffer { get; }
    //sorted by player index in the room, the inserted chars that are not processed

    public string[] Words { get; }

    public Room(int capacityChoice, int category, string[] words)
    {
        CapacityChoice = capacityChoice;
        Words = words;
        Category = category;

        CharBuffer = new List<char>[capacityChoice];
    }
}