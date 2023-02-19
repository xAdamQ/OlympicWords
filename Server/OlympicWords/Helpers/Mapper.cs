using System;
using System.Linq.Expressions;
using OlympicWords.Common;
using OlympicWords.Data;

namespace OlympicWords.Services.Helpers
{
    public static class Mapper
    {
        public static Expression<Func<User, PersonalFullUserInfo>> UserToClientUserProjection =>
            u => new PersonalFullUserInfo
            {
                Id = u.Id,
                Money = u.Money,
                PlayedRoomsCount = u.PlayedRoomsCount,
                Name = u.Name,
                PictureUrl = u.PictureUrl,
                WonRoomsCount = u.WonRoomsCount,
                AverageWpm = u.AverageWpm,
                LastMoneyAimRequestTime = u.LastMoneyAimRequestTime,
                MoneyAimTimePassed = u.LastMoneyAimRequestTime == null
                    ? null
                    : (DateTime.UtcNow - u.LastMoneyAimRequestTime).Value.TotalSeconds,
                OwnedCardBackIds = u.OwnedCardBackIds,
                SelectedCardback = u.SelectedCardback,
                SelectedBackground = u.SelectedBackground,
                OwnedTitleIds = u.OwnedTitleIds,
                WinStreak = u.WinStreak,
                EatenCardsCount = u.EatenCardsCount,
                OwnedBackgroundsIds = u.OwnedBackgroundIds,
                SelectedTitleId = u.SelectedTitleId,
                TotalEarnedMoney = u.TotalEarnedMoney,
                Xp = u.Xp,
                MaxWinStreak = u.MaxWinStreak,
                RequestedMoneyAidToday = u.RequestedMoneyAidToday,
                // OwnedPlayers = u.OwnedPlayers,
                EnableOpenMatches = u.EnableOpenMatches,
                Followers = u.Followers.Select(f => new MinUserInfo
                {
                    Name = f.Name,
                }).ToList(),
                Followings = u.Followings.Select(f => new MinUserInfo
                {
                    Name = f.Name,
                }).ToList(),
            };

        public static Expression<Func<User, FullUserInfo>> UserToFullProjection =>
            u => new FullUserInfo
            {
                Id = u.Id,
                Name = u.Name,
                PlayedRoomsCount = u.PlayedRoomsCount,
                WonRoomsCount = u.WonRoomsCount,
                AverageWpm = u.AverageWpm,
                EatenCardsCount = u.EatenCardsCount,
                WinStreak = u.WinStreak,
                OwnedBackgroundsIds = u.OwnedBackgroundIds,
                OwnedCardBackIds = u.OwnedCardBackIds,
                TotalEarnedMoney = u.TotalEarnedMoney,
                SelectedTitleId = u.SelectedTitleId,
                PictureUrl = u.PictureUrl,
                SelectedBackground = u.SelectedBackground,
                Money = u.Money,
                SelectedCardback = u.SelectedCardback,
                MaxWinStreak = u.MaxWinStreak,
                Xp = u.Xp,
                EnableOpenMatches = u.EnableOpenMatches,
            };

        public static Expression<Func<User, MinUserInfo>> UserToMinProjection =>
            u => new MinUserInfo
            {
                Id = u.Id,
                Name = u.Name,
                SelectedTitleId = u.SelectedTitleId,
                PictureUrl = u.PictureUrl,
                Xp = u.Xp,
            };

        public static readonly Func<User, FullUserInfo> UserToFullFunc =
            UserToFullProjection.Compile();

        public static readonly Func<User, MinUserInfo> UserToMinFunc =
            UserToMinProjection.Compile();

        public static readonly Func<User, PersonalFullUserInfo> UserToClientUserFunc =
            UserToClientUserProjection.Compile();
    }
}