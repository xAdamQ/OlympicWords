using System.ComponentModel.DataAnnotations.Schema;
using OlympicWords.Data;
using OlympicWords.Services;

// ReSharper disable ClassWithVirtualMembersNeverInherited.Global


namespace OlympicWords.Data
{
    public class User
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        public int PlayedRoomsCount { get; set; }
        public int WonRoomsCount { get; set; }

        public int Money { get; set; }

        public string Name { get; set; }
        public string Email { get; set; }

        public string PictureUrl { get; set; }

        public DateTime LastLogin { get; set; }

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

        public virtual List<ProviderLink> Providers { get; set; }

        /// <summary>
        /// relations were I am the follower
        /// </summary>
        public virtual List<UserRelation> FollowingRelations { get; set; }
        /// <summary>
        /// relations were I am being followed
        /// </summary>
        public virtual List<UserRelation> FollowerRelations { get; set; }

        public virtual List<User> Followings { get; set; }
        public virtual List<User> Followers { get; set; }
        //you can't add following somewhere, without follower??
        
        public virtual UserPicture Picture { get; set; }
    }
}

namespace OlympicWords.Services
{
    public class UserRelation
    {
        public string FollowerId { get; set; }
        public string FollowingId { get; set; }

        public virtual User Follower { get; set; }
        public virtual User Following { get; set; }
    }
}