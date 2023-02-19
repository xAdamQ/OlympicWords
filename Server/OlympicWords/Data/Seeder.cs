using Microsoft.EntityFrameworkCore;
using OlympicWords.Data;

namespace OlympicWords.Services;
public static class Seeder
{
    public static void SeedData(ModelBuilder modelBuilder)
    {
        var selectedPlayers =
            new Dictionary<string, string>
            {
                {
                    "GraphJumpCity", OfflineRepo.ItemPlayers.First().Value.Id
                }
            };
        var ownedPlayers = new HashSet<string>
        {
            OfflineRepo.ItemPlayers.First().Value.Id,
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
            SelectedCardback = 1,
            OwnedItemPlayers = ownedPlayers,
            SelectedItemPlayer = selectedPlayers,
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
            SelectedCardback = 2,
            OwnedItemPlayers = ownedPlayers,
            SelectedItemPlayer = selectedPlayers,
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
            OwnedItemPlayers = ownedPlayers,
            SelectedItemPlayer = selectedPlayers,
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

        modelBuilder.Entity<User>().HasData(
            new List<User>
            {
                bot999,
                bot9999,
                bot99999,
            }
        );
    }
}