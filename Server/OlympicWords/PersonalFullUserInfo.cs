using System;
using System.Collections.Generic;

namespace OlympicWords.Common
{
    public class PersonalFullUserInfo : FullUserInfo
    {
        public double? MoneyAimTimePassed { get; set; }

        public int RequestedMoneyAidToday { get; set; }

        public List<int> OwnedTitleIds { get; set; }

        public DateTime? LastMoneyAimRequestTime { get; set; }

        public List<MinUserInfo> Followers { get; set; }
        public List<MinUserInfo> Followings { get; set; }

        public HashSet<string> OwnedItemPlayers { get; set; }
        public Dictionary<string, string> SelectedItemPlayer { get; set; }
    }
}