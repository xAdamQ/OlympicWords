using Microsoft.EntityFrameworkCore;
using OlympicWords.Data;

namespace OlympicWords.Services;

public static class Seeder
{
    public static void SeedData(ModelBuilder modelBuilder)
    {
        var user0 = new User()
        {
            Id = "0",
            PlayedRoomsCount = 3,
            WonRoomsCount = 4,
            Name = "hany",
            OwnedBackgroundIds = new List<int> { 1, 3 },
            OwnedTitleIds = new List<int> { 2, 4 },
            PictureUrl = "https://pbs.twimg.com/profile_images/592734306725933057/s4-h_LQC.jpg",
            Level = 13,
            Money = 22250,
            Xp = 806,
            OwnedCardBackIds = new List<int>() { 0, 2 },
            RequestedMoneyAidToday = 2,
            LastMoneyAimRequestTime = null,
            SelectedCardback = 2,
        };

        var bot999 = new User()
        {
            Id = "999",
            PlayedRoomsCount = 9,
            WonRoomsCount = 2,
            Name = "botA",
            PictureUrl =
                "https://pbs.twimg.com/profile_images/723902674970750978/p8JWhWxP_400x400.jpg",
            OwnedBackgroundIds = new List<int> { 0, 3 },
            OwnedTitleIds = new List<int> { 1 },
            OwnedCardBackIds = new List<int> { 8 },
            Level = 7,
            Money = 1000,
            Xp = 34,
            RequestedMoneyAidToday = 0,
            LastMoneyAimRequestTime = null,
            SelectedCardback = 1
        };
        var bot9999 = new User()
        {
            Id = "9999",
            PlayedRoomsCount = 11,
            WonRoomsCount = 3,
            Name = "botB",
            PictureUrl = "https://pbs.twimg.com/profile_images/592734306725933057/s4-h_LQC.jpg",
            OwnedBackgroundIds = new List<int> { 3 },
            OwnedTitleIds = new List<int> { 0, 1 },
            OwnedCardBackIds = new List<int> { 0, 8 },
            Level = 8,
            Money = 1100,
            Xp = 44,
            RequestedMoneyAidToday = 0,
            LastMoneyAimRequestTime = null,
            SelectedCardback = 2
        };
        var bot99999 = new User()
        {
            Id = "99999",
            PlayedRoomsCount = 11,
            WonRoomsCount = 3,
            Name = "botC",
            PictureUrl =
                "https://d3g9pb5nvr3u7.cloudfront.net/authors/57ea8955d8de1e1602f67ca0/1902081322/256.jpg",
            OwnedBackgroundIds = new List<int> { 3 },
            OwnedTitleIds = new List<int> { 0, 1 },
            OwnedCardBackIds = new List<int> { 0, 8 },
            Level = 8,
            Xp = 44,
            RequestedMoneyAidToday = 0,
            LastMoneyAimRequestTime = null,
            SelectedCardback = 2,
        };

        var relations = new List<UserRelation>()
        {
            new() { FollowerId = user0.Id, FollowingId = bot999.Id },
            new() { FollowerId = user0.Id, FollowingId = bot9999.Id },
            new() { FollowerId = user0.Id, FollowingId = bot99999.Id },
        };

        var pictureData = new UserPicture[]
        {
            new()
            {
                UserId = "999",
                AvatarId = 1,
            },
            new()
            {
                UserId = "9999",
                AvatarId = 2,
            },
            new()
            {
                UserId = "99999",
                AvatarId = 3,
            },
        };

        modelBuilder.Entity<UserPicture>().HasData(pictureData);

        modelBuilder.Entity<UserRelation>().HasData(relations);

        modelBuilder.Entity<User>().HasData(
            new List<User>
            {
                user0,
                bot999,
                bot9999,
                bot99999,
                new()
                {
                    Id = "1",
                    PlayedRoomsCount = 7,
                    WonRoomsCount = 11,
                    Name = "samy",
                    OwnedBackgroundIds = new List<int> { 0, 9 },
                    OwnedTitleIds = new List<int> { 11, 6 },
                    PictureUrl =
                        "https://d3g9pb5nvr3u7.cloudfront.net/authors/57ea8955d8de1e1602f67ca0/1902081322/256.jpg",
                    Level = 43,
                    Money = 89000,
                    Xp = 1983,
                    OwnedCardBackIds = new List<int>() { 0, 1, 2 },
                    RequestedMoneyAidToday = 0,
                    LastMoneyAimRequestTime = null,
                    SelectedCardback = 1,
                },
                new()
                {
                    Id = "2",
                    PlayedRoomsCount = 973,
                    WonRoomsCount = 192,
                    Name = "anni",
                    OwnedBackgroundIds = new List<int> { 10, 8 },
                    OwnedTitleIds = new List<int> { 1, 3 },
                    OwnedCardBackIds = new List<int> { 4, 9 },
                    PictureUrl =
                        "https://pbs.twimg.com/profile_images/633661532350623745/8U1sJUc8_400x400.png",
                    Level = 139,
                    Money = 8500,
                    Xp = 8062,
                    RequestedMoneyAidToday = 4,
                    LastMoneyAimRequestTime = null,
                    SelectedCardback = 4,
                },
                new()
                {
                    Id = "3",
                    PlayedRoomsCount = 6,
                    WonRoomsCount = 2,
                    Name = "ali",
                    PictureUrl =
                        "https://pbs.twimg.com/profile_images/723902674970750978/p8JWhWxP_400x400.jpg",
                    OwnedBackgroundIds = new List<int> { 10, 8 },
                    OwnedTitleIds = new List<int> { 1, 3 },
                    OwnedCardBackIds = new List<int> { 2, 4, 8 },
                    Level = 4,
                    Money = 3,
                    Xp = 12,
                    RequestedMoneyAidToday = 3,
                    LastMoneyAimRequestTime = null,
                    SelectedCardback = 2
                },
            }
        );

        // modelBuilder.Entity<UserRelation>().HasData(
        //     new List<UserRelation>()
        //     {
        //         new()
        //         {
        //             FollowerId = "0",
        //             FollowingId = "999",
        //         },
        //         new()
        //         {
        //             FollowerId = "0",
        //             FollowingId = "9999",
        //         },
        //
        //         new()
        //         {
        //             FollowerId = "9999",
        //             FollowingId = "0",
        //         }
        //     }
        // );
    }
}