using Microsoft.EntityFrameworkCore.Migrations;

namespace x42.Migrations
{
    public partial class AddedNetworkProtocol : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NetworkProtocol",
                table: "servernode",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NetworkProtocol",
                table: "servernode");
        }
    }
}
