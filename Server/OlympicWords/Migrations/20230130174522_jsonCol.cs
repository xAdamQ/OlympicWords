using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OlympicWords.Migrations
{
    /// <inheritdoc />
    public partial class jsonCol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "UserRelation",
                keyColumns: new[] { "FollowerId", "FollowingId" },
                keyValues: new object[] { "0", "999" });

            migrationBuilder.DeleteData(
                table: "UserRelation",
                keyColumns: new[] { "FollowerId", "FollowingId" },
                keyValues: new object[] { "0", "9999" });

            migrationBuilder.DeleteData(
                table: "UserRelation",
                keyColumns: new[] { "FollowerId", "FollowingId" },
                keyValues: new object[] { "0", "99999" });

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "1");

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "2");

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "3");

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "0");

            migrationBuilder.AddColumn<string>(
                name: "ItemPlayers",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnedPlayers",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedPlayers",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemPlayers",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OwnedPlayers",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SelectedPlayers",
                table: "Users");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AverageWpm", "EatenCardsCount", "Email", "EnableOpenMatches", "LastLogin", "LastMoneyAimRequestTime", "Level", "MaxWinStreak", "Money", "Name", "OwnedBackgroundIds", "OwnedCardBackIds", "OwnedTitleIds", "PictureUrl", "PlayedRoomsCount", "RequestedMoneyAidToday", "SelectedBackground", "SelectedCardback", "SelectedTitleId", "TotalEarnedMoney", "WinStreak", "WonRoomsCount", "Xp" },
                values: new object[,]
                {
                    { "0", 0f, 0, null, false, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 13, 0, 22250, "hany", "[1,3]", "[0,2]", "[2,4]", "https://pbs.twimg.com/profile_images/592734306725933057/s4-h_LQC.jpg", 3, 2, 0, 2, 0, 0, 0, 4, 806 },
                    { "1", 0f, 0, null, false, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 43, 0, 89000, "samy", "[0,9]", "[0,1,2]", "[11,6]", "https://d3g9pb5nvr3u7.cloudfront.net/authors/57ea8955d8de1e1602f67ca0/1902081322/256.jpg", 7, 0, 0, 1, 0, 0, 0, 11, 1983 },
                    { "2", 0f, 0, null, false, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 139, 0, 8500, "anni", "[10,8]", "[4,9]", "[1,3]", "https://pbs.twimg.com/profile_images/633661532350623745/8U1sJUc8_400x400.png", 973, 4, 0, 4, 0, 0, 0, 192, 8062 },
                    { "3", 0f, 0, null, false, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 4, 0, 3, "ali", "[10,8]", "[2,4,8]", "[1,3]", "https://pbs.twimg.com/profile_images/723902674970750978/p8JWhWxP_400x400.jpg", 6, 3, 0, 2, 0, 0, 0, 2, 12 }
                });

            migrationBuilder.InsertData(
                table: "UserRelation",
                columns: new[] { "FollowerId", "FollowingId" },
                values: new object[,]
                {
                    { "0", "999" },
                    { "0", "9999" },
                    { "0", "99999" }
                });
        }
    }
}
