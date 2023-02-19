using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OlympicWords.Migrations
{
    public partial class avgWpm : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "AverageWpm",
                table: "Users",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageWpm",
                table: "Users");
        }
    }
}
