using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OlympicWords.Migrations
{
    public partial class exteranlIdToProvider : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "UserPictures",
                columns: new[] { "UserId", "AvatarId", "Picture" },
                values: new object[] { "999", 1, null });

            migrationBuilder.InsertData(
                table: "UserPictures",
                columns: new[] { "UserId", "AvatarId", "Picture" },
                values: new object[] { "9999", 2, null });

            migrationBuilder.InsertData(
                table: "UserPictures",
                columns: new[] { "UserId", "AvatarId", "Picture" },
                values: new object[] { "99999", 3, null });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "UserPictures",
                keyColumn: "UserId",
                keyValue: "999");

            migrationBuilder.DeleteData(
                table: "UserPictures",
                keyColumn: "UserId",
                keyValue: "9999");

            migrationBuilder.DeleteData(
                table: "UserPictures",
                keyColumn: "UserId",
                keyValue: "99999");
        }
    }
}
