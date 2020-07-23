using Microsoft.EntityFrameworkCore.Migrations;

namespace x42.Migrations
{
    public partial class FixedConfirm : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_profile_BlockConfirmed",
                table: "profile");

            migrationBuilder.CreateIndex(
                name: "IX_profile_BlockConfirmed",
                table: "profile",
                column: "BlockConfirmed");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_profile_BlockConfirmed",
                table: "profile");

            migrationBuilder.CreateIndex(
                name: "IX_profile_BlockConfirmed",
                table: "profile",
                column: "BlockConfirmed",
                unique: true);
        }
    }
}
