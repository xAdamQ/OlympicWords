using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OlympicWords.Migrations
{
    /// <inheritdoc />
    public partial class ownedselectedplayersmanuallynocomparer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemPlayers",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "SelectedPlayers",
                table: "Users",
                newName: "SelectedItemPlayer");

            migrationBuilder.RenameColumn(
                name: "OwnedPlayers",
                table: "Users",
                newName: "OwnedItemPlayers");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "999",
                columns: new[] { "OwnedItemPlayers", "SelectedItemPlayer" },
                values: new object[] { "[\"criminal\"]", "{\"Base\":\"criminal\"}" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "9999",
                columns: new[] { "OwnedItemPlayers", "SelectedItemPlayer" },
                values: new object[] { "[\"criminal\"]", "{\"Base\":\"criminal\"}" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "99999",
                columns: new[] { "OwnedItemPlayers", "SelectedItemPlayer" },
                values: new object[] { "[\"criminal\"]", "{\"Base\":\"criminal\"}" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SelectedItemPlayer",
                table: "Users",
                newName: "SelectedPlayers");

            migrationBuilder.RenameColumn(
                name: "OwnedItemPlayers",
                table: "Users",
                newName: "OwnedPlayers");

            migrationBuilder.AddColumn<string>(
                name: "ItemPlayers",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
