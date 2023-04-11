using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;

public class FullUserInfo : MinUserInfo
{
    [Preserve]
    public FullUserInfo()
    {
    }

    public virtual int Money { get; set; }

    public int PlayedRoomsCount { get; set; }
    public int WonRoomsCount { get; set; }
    public int TotalEarnedMoney { get; set; }

    public int WinStreak { get; set; }
    public int MaxWinStreak { get; set; }

    public List<int> OwnedCardBackIds { get; set; }
    public List<int> OwnedBackgroundsIds { get; set; }
    public int SelectedCardback { get; set; }
    public int SelectedBackground { get; set; }

    public bool EnableOpenMatches { get; set; }

    public int Friendship { get; set; }

    public List<string> OwnedItemPlayers { get; set; }
    public Dictionary<string, string> SelectedItemPlayer { get; set; }

    #region helpers
    public List<int>[] OwnedItemIds => new[] { OwnedCardBackIds, OwnedBackgroundsIds };
    public int[] SelectedItem => new[] { SelectedCardback, SelectedBackground };
    public float WinRatio => (float)WonRoomsCount / PlayedRoomsCount;
    #endregion
}