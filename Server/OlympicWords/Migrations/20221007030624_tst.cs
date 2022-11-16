using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OlympicWords.Migrations
{
    public partial class tst : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserUser");

            migrationBuilder.AddColumn<int>(
                name: "AvatarId",
                table: "UserPictures",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "UserRelation",
                columns: table => new
                {
                    FollowerId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FollowingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRelation", x => new { x.FollowerId, x.FollowingId });
                    table.ForeignKey(
                        name: "FK_UserRelation_Users_FollowerId",
                        column: x => x.FollowerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRelation_Users_FollowingId",
                        column: x => x.FollowingId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "UserRelation",
                columns: new[] { "FollowerId", "FollowingId" },
                values: new object[] { "0", "999" });

            migrationBuilder.InsertData(
                table: "UserRelation",
                columns: new[] { "FollowerId", "FollowingId" },
                values: new object[] { "0", "9999" });

            migrationBuilder.InsertData(
                table: "UserRelation",
                columns: new[] { "FollowerId", "FollowingId" },
                values: new object[] { "0", "99999" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRelation_FollowingId",
                table: "UserRelation",
                column: "FollowingId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRelation");

            migrationBuilder.DropColumn(
                name: "AvatarId",
                table: "UserPictures");

            migrationBuilder.CreateTable(
                name: "UserUser",
                columns: table => new
                {
                    FollowersId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FollowingsId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
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
                name: "IX_UserUser_FollowingsId",
                table: "UserUser",
                column: "FollowingsId");
        }
    }
}
