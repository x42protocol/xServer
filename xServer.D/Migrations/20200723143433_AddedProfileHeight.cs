using Microsoft.EntityFrameworkCore.Migrations;

namespace x42.Migrations
{
    public partial class AddedProfileHeight : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProfileHeight",
                table: "server",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "ReservationExpirationBlock",
                table: "profilereservation",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<int>(
                name: "BlockConfirmed",
                table: "profile",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "ExpireBlock",
                table: "pricelock",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateIndex(
                name: "IX_profile_BlockConfirmed",
                table: "profile",
                column: "BlockConfirmed",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_profile_BlockConfirmed",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "ProfileHeight",
                table: "server");

            migrationBuilder.DropColumn(
                name: "BlockConfirmed",
                table: "profile");

            migrationBuilder.AlterColumn<long>(
                name: "ReservationExpirationBlock",
                table: "profilereservation",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<long>(
                name: "ExpireBlock",
                table: "pricelock",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int));
        }
    }
}
