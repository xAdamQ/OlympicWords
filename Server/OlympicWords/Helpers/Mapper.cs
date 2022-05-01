using System;
using System.Linq.Expressions;
using Basra.Common;

namespace OlympicWords.Services.Helpers
{
    public static class Mapper
    {
        public static PersonalFullUserInfo ConvertUserDataToClient(User u)
        {
            return new()
            {
                Id = u.Id,
                Money = u.Money,
                PlayedRoomsCount = u.PlayedRoomsCount,
                Name = u.Name,
                PictureUrl = u.PictureUrl,
                WonRoomsCount = u.WonRoomsCount,
                LastMoneyAimRequestTime = u.LastMoneyAimRequestTime,
                MoneyAimTimePassed =
                    u.LastMoneyAimRequestTime == null
                        ? null
                        : (DateTime.UtcNow - u.LastMoneyAimRequestTime).Value.TotalSeconds,
                OwnedCardBackIds = u.OwnedCardBackIds,
                SelectedCardback = u.SelectedCardback,
                SelectedBackground = u.SelectedBackground,
                DrawRoomsCount = u.Draws,
                Titles = u.OwnedTitleIds,
                BasraCount = u.BasraCount,
                WinStreak = u.WinStreak,
                BigBasraCount = u.BigBasraCount,
                EatenCardsCount = u.EatenCardsCount,
                OwnedBackgroundsIds = u.OwnedBackgroundIds,
                SelectedTitleId = u.SelectedTitleId,
                TotalEarnedMoney = u.TotalEarnedMoney,
                Xp = u.Xp,
                MaxWinStreak = u.MaxWinStreak,
                MoneyAidRequested = u.RequestedMoneyAidToday,
                EnableOpenMatches = u.EnableOpenMatches,
            };
        }

        public static Expression<Func<User, FullUserInfo>> UserToFullUserInfoProjection =>
            u => new FullUserInfo
            {
                Id = u.Id,
                Name = u.Name,
                PlayedRoomsCount = u.PlayedRoomsCount,
                WonRoomsCount = u.WonRoomsCount,
                EatenCardsCount = u.EatenCardsCount,
                WinStreak = u.WinStreak,
                BasraCount = u.BasraCount,
                BigBasraCount = u.BigBasraCount,
                OwnedBackgroundsIds = u.OwnedBackgroundIds,
                OwnedCardBackIds = u.OwnedCardBackIds,
                TotalEarnedMoney = u.TotalEarnedMoney,
                SelectedTitleId = u.SelectedTitleId,
                PictureUrl = u.PictureUrl,
                SelectedBackground = u.SelectedBackground,
                Money = u.Money,
                SelectedCardback = u.SelectedCardback,
                DrawRoomsCount = u.Draws,
                MaxWinStreak = u.MaxWinStreak,
                Xp = u.Xp,
                EnableOpenMatches = u.EnableOpenMatches,
            };

        public static Expression<Func<User, MinUserInfo>> UserToMinUserInfoProjection =>
            u => new MinUserInfo
            {
                Id = u.Id,
                Name = u.Name,
                SelectedTitleId = u.SelectedTitleId,
                PictureUrl = u.PictureUrl,
                Xp = u.Xp,
            };

        public static Func<User, FullUserInfo> UserToFullUserInfoFunc =
            UserToFullUserInfoProjection.Compile();
        public static Func<User, MinUserInfo> UserToMinUserInfoFunc =
            UserToMinUserInfoProjection.Compile();
    }
}