using System.Collections.Generic;
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
    public int DrawRoomsCount { get; set; }
    public int EatenCardsCount { get; set; }
    public int TotalEarnedMoney { get; set; }

    public int WinStreak { get; set; }
    public int MaxWinStreak { get; set; }

    public int BasraCount { get; set; }
    public int BigBasraCount { get; set; }


    //client helpers only?
    // public List<int> OwnedCardBackIds => OwnedItemIds[(int) ItemType.Cardback];
    // public List<int> OwnedBackgroundsIds => OwnedItemIds[(int) ItemType.Background];
    // public int SelectedCardback => SelectedItem[(int) ItemType.Cardback];
    // public int SelectedBackground => SelectedItem[(int) ItemType.Background];

    public List<int> OwnedCardBackIds { get; set; }
    public List<int> OwnedBackgroundsIds { get; set; }
    public int SelectedCardback { get; set; }
    public int SelectedBackground { get; set; }

    public bool EnableOpenMatches { get; set; }

    public int Friendship { get; set; }


    #region helpers

    public List<int>[] OwnedItemIds => new[] { OwnedCardBackIds, OwnedBackgroundsIds };
    public int[] SelectedItem => new[] { SelectedCardback, SelectedBackground };

    public float WinRatio => (float)WonRoomsCount / PlayedRoomsCount;

    #endregion
}