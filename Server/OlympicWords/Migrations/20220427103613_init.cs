using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OlymbicWords.Migrations
{
    public partial class init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalIds",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    MainId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalIds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRelations",
                columns: table => new
                {
                    FollowerId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FollowingId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRelations", x => new { x.FollowerId, x.FollowingId });
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PlayedRoomsCount = table.Column<int>(type: "int", nullable: false),
                    WonRoomsCount = table.Column<int>(type: "int", nullable: false),
                    Draws = table.Column<int>(type: "int", nullable: false),
                    Money = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PictureUrl = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    EatenCardsCount = table.Column<int>(type: "int", nullable: false),
                    WinStreak = table.Column<int>(type: "int", nullable: false),
                    MaxWinStreak = table.Column<int>(type: "int", nullable: false),
                    BasraCount = table.Column<int>(type: "int", nullable: false),
                    BigBasraCount = table.Column<int>(type: "int", nullable: false),
                    TotalEarnedMoney = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Xp = table.Column<int>(type: "int", nullable: false),
                    RequestedMoneyAidToday = table.Column<int>(type: "int", nullable: false),
                    LastMoneyAimRequestTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OwnedBackgroundIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnedCardBackIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnedTitleIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SelectedTitleId = table.Column<int>(type: "int", nullable: false),
                    SelectedCardback = table.Column<int>(type: "int", nullable: false),
                    SelectedBackground = table.Column<int>(type: "int", nullable: false),
                    EnableOpenMatches = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "UserRelations",
                columns: new[] { "FollowerId", "FollowingId" },
                values: new object[,]
                {
                    { "0", "999" },
                    { "0", "9999" },
                    { "9999", "0" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "BasraCount", "BigBasraCount", "Draws", "EatenCardsCount", "Email", "EnableOpenMatches", "LastMoneyAimRequestTime", "Level", "MaxWinStreak", "Money", "Name", "OwnedBackgroundIds", "OwnedCardBackIds", "OwnedTitleIds", "PictureUrl", "PlayedRoomsCount", "RequestedMoneyAidToday", "SelectedBackground", "SelectedCardback", "SelectedTitleId", "TotalEarnedMoney", "WinStreak", "WonRoomsCount", "Xp" },
                values: new object[,]
                {
                    { "0", 0, 0, 3, 0, null, false, null, 13, 0, 22250, "hany", "[1,3]", "[0,2]", "[2,4]", "https://pbs.twimg.com/profile_images/592734306725933057/s4-h_LQC.jpg", 3, 2, 0, 2, 0, 0, 0, 4, 806 },
                    { "1", 0, 0, 1, 0, null, false, null, 43, 0, 89000, "samy", "[0,9]", "[0,1,2]", "[11,6]", "https://d3g9pb5nvr3u7.cloudfront.net/authors/57ea8955d8de1e1602f67ca0/1902081322/256.jpg", 7, 0, 0, 1, 0, 0, 0, 11, 1983 },
                    { "2", 0, 0, 37, 0, null, false, null, 139, 0, 8500, "anni", "[10,8]", "[4,9]", "[1,3]", "https://pbs.twimg.com/profile_images/633661532350623745/8U1sJUc8_400x400.png", 973, 4, 0, 4, 0, 0, 0, 192, 8062 },
                    { "3", 0, 0, 1, 0, null, false, null, 4, 0, 3, "ali", "[10,8]", "[2,4,8]", "[1,3]", "https://pbs.twimg.com/profile_images/723902674970750978/p8JWhWxP_400x400.jpg", 6, 3, 0, 2, 0, 0, 0, 2, 12 },
                    { "999", 0, 0, 2, 0, null, false, null, 7, 0, 1000, "botA", "[0,3]", "[8]", "[1]", "https://pbs.twimg.com/profile_images/723902674970750978/p8JWhWxP_400x400.jpg", 9, 0, 0, 1, 0, 0, 0, 2, 34 },
                    { "9999", 0, 0, 2, 0, null, false, null, 8, 0, 1100, "botB", "[3]", "[0,8]", "[0,1]", "https://pbs.twimg.com/profile_images/592734306725933057/s4-h_LQC.jpg", 11, 0, 0, 2, 0, 0, 0, 3, 44 },
                    { "99999", 0, 0, 2, 0, null, false, null, 8, 0, 0, "botC", "[3]", "[0,8]", "[0,1]", "https://d3g9pb5nvr3u7.cloudfront.net/authors/57ea8955d8de1e1602f67ca0/1902081322/256.jpg", 11, 0, 0, 2, 0, 0, 0, 3, 44 }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalIds");

            migrationBuilder.DropTable(
                name: "UserRelations");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
