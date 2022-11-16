using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OlympicWords.Migrations
{
    public partial class removeExcplicitRelations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRelations");

            migrationBuilder.RenameColumn(
                name: "MainId",
                table: "ExternalIds",
                newName: "UserId");

            migrationBuilder.CreateTable(
                name: "UserUser",
                columns: table => new
                {
                    FollowersId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FollowingsId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserUser", x => new { x.FollowersId, x.FollowingsId });
                    table.ForeignKey(
                        name: "FK_UserUser_Users_FollowersId",
                        column: x => x.FollowersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserUser_Users_FollowingsId",
                        column: x => x.FollowingsId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIds_UserId",
                table: "ExternalIds",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserUser_FollowingsId",
                table: "UserUser",
                column: "FollowingsId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExternalIds_Users_UserId",
                table: "ExternalIds",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExternalIds_Users_UserId",
                table: "ExternalIds");

            migrationBuilder.DropTable(
                name: "UserUser");

            migrationBuilder.DropIndex(
                name: "IX_ExternalIds_UserId",
                table: "ExternalIds");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "ExternalIds",
                newName: "MainId");

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

            migrationBuilder.InsertData(
                table: "UserRelations",
                columns: new[] { "FollowerId", "FollowingId" },
                values: new object[] { "0", "999" });

            migrationBuilder.InsertData(
                table: "UserRelations",
                columns: new[] { "FollowerId", "FollowingId" },
                values: new object[] { "0", "9999" });

            migrationBuilder.InsertData(
                table: "UserRelations",
                columns: new[] { "FollowerId", "FollowingId" },
                values: new object[] { "9999", "0" });
        }
    }
}
