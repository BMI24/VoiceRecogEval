using Microsoft.EntityFrameworkCore.Migrations;

namespace VoiceRecogEvalServer.Migrations
{
    public partial class Migration3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasUserError",
                table: "Recordings",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasUserError",
                table: "Recordings");
        }
    }
}
