using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OlympicWords.Services
{
    public class User
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        public int PlayedRoomsCount { get; set; }
        public int WonRoomsCount { get; set; }

        public int Money { get; set; }

        public string Name { get; set; }

        public string PictureUrl { get; set; }

        //states
        public int EatenCardsCount { get; set; }
        public int WinStreak { get; set; }
        public int MaxWinStreak { get; set; }
        public int TotalEarnedMoney { get; set; }

        public int Level { get; set; }
        public int Xp { get; set; }

        /// <summary>
        /// amount of requested aids today
        /// </summary>
        public int RequestedMoneyAidToday { get; set; }

        /// <summary>
        /// when it was requested, used to help the client see remaining time
        /// after reconnect
        /// </summary>
        public DateTime? LastMoneyAimRequestTime { get; set; }

        /// <summary>
        /// the player is waiting 15 minutes to get the money
        /// </summary>
        public bool IsMoneyAidProcessing => LastMoneyAimRequestTime != null;

        public List<int> OwnedBackgroundIds { get; set; } = new();
        public List<int> OwnedCardBackIds { get; set; } = new();
        public List<int> OwnedTitleIds { get; set; } = new();

        public int SelectedTitleId { get; set; }
        public int SelectedCardback { get; set; }
        public int SelectedBackground { get; set; }

        /// <summary>
        /// if true, anyone challenge the user
        /// if false, people who he follows only can challenge him
        /// </summary>
        public bool EnableOpenMatches { get; set; }
    }

    public class UserRelation
    {
        [ForeignKey("User")] public string FollowerId { get; set; }
        [ForeignKey("User")] public string FollowingId { get; set; }
    }
}