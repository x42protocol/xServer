using Microsoft.EntityFrameworkCore.Migrations;

namespace x42.Migrations
{
    public partial class AddedKeyAddress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KeyAddress",
                table: "server",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KeyAddress",
                table: "server");
        }
    }
}
