using System;
using System.Collections.Generic;

namespace Basra.Common
{
    public class PersonalFullUserInfo : FullUserInfo
    {
        public double? MoneyAimTimePassed { get; set; }

        public int MoneyAidRequested { get; set; }

        public int FlipWinCount { get; set; }

        public List<int> Titles { get; set; }

        public DateTime? LastMoneyAimRequestTime { get; set; }

        public List<MinUserInfo> Followers { get; set; }
        public List<MinUserInfo> Followings { get; set; }
    }
}