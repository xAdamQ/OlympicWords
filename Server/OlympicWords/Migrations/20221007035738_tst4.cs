using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OlympicWords.Migrations
{
    public partial class tst4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UserPictures",
                table: "UserPictures");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "UserPictures");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "UserPictures",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserPictures",
                table: "UserPictures",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserPictures_Users_UserId",
                table: "UserPictures",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserPictures_Users_UserId",
                table: "UserPictures");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserPictures",
                table: "UserPictures");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserPictures");

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "UserPictures",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserPictures",
                table: "UserPictures",
                column: "Id");
        }
    }
}
