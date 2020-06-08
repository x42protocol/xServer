using Microsoft.EntityFrameworkCore.Migrations;

namespace x42.Migrations
{
    public partial class AddedRelayed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Relayed",
                table: "servernode",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Relayed",
                table: "servernode");
        }
    }
}
