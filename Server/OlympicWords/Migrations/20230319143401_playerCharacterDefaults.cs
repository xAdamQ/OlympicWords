using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OlympicWords.Migrations
{
    /// <inheritdoc />
    public partial class playerCharacterDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "999",
                columns: new[] { "OwnedItemPlayers", "SelectedItemPlayer" },
                values: new object[] { "[\"criminal\",\"female\"]", "{\"MJC\":\"criminal\",\"MWSC\":\"female\"}" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "9999",
                columns: new[] { "OwnedItemPlayers", "SelectedItemPlayer" },
                values: new object[] { "[\"criminal\",\"female\"]", "{\"MJC\":\"criminal\",\"MWSC\":\"female\"}" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "99999",
                columns: new[] { "OwnedItemPlayers", "SelectedItemPlayer" },
                values: new object[] { "[\"criminal\",\"female\"]", "{\"MJC\":\"criminal\",\"MWSC\":\"female\"}" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
