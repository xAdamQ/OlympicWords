using System.Collections.Generic;

namespace Basra.Common
{
    public class FullUserInfo : MinUserInfo
    {
        public int Money { get; set; }
        public int PlayedRoomsCount { get; set; }
        public int WonRoomsCount { get; set; }
        public int DrawRoomsCount { get; set; }
        public int EatenCardsCount { get; set; }
        public int WinStreak { get; set; }
        public int MaxWinStreak { get; set; }
        public int BasraCount { get; set; }
        public int BigBasraCount { get; set; }
        public int TotalEarnedMoney { get; set; }

        public List<int> OwnedCardBackIds { get; set; }
        public List<int> OwnedBackgroundsIds { get; set; }

        public int SelectedCardback { get; set; }
        public int SelectedBackground { get; set; }

        public bool EnableOpenMatches { get; set; }

        public int Friendship { get; set; }
    }
}