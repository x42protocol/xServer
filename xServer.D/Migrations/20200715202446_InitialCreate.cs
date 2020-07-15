using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace x42.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pricelock",
                columns: table => new
                {
                    PriceLockId = table.Column<Guid>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    RequestAmount = table.Column<decimal>(nullable: false),
                    RequestAmountPair = table.Column<int>(nullable: false),
                    FeeAmount = table.Column<decimal>(nullable: false),
                    FeeAddress = table.Column<string>(nullable: true),
                    DestinationAmount = table.Column<decimal>(nullable: false),
                    DestinationAddress = table.Column<string>(nullable: true),
                    TransacrionId = table.Column<string>(nullable: true),
                    PriceLockSignature = table.Column<string>(nullable: true),
                    PayeeSignature = table.Column<string>(nullable: true),
                    ExpireBlock = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pricelock", x => x.PriceLockId);
                });

            migrationBuilder.CreateTable(
                name: "profile",
                columns: table => new
                {
                    Name = table.Column<string>(nullable: false),
                    KeyAddress = table.Column<string>(nullable: true),
                    Signature = table.Column<string>(nullable: true),
                    PriceLockId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "server",
                columns: table => new
                {
                    ProfileName = table.Column<string>(nullable: false),
                    SignAddress = table.Column<string>(nullable: true),
                    DateAdded = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server", x => x.ProfileName);
                });

            migrationBuilder.CreateTable(
                name: "servernode",
                columns: table => new
                {
                    ProfileName = table.Column<string>(nullable: false),
                    NetworkProtocol = table.Column<int>(nullable: false),
                    NetworkAddress = table.Column<string>(nullable: true),
                    NetworkPort = table.Column<long>(nullable: false),
                    KeyAddress = table.Column<string>(nullable: true),
                    SignAddress = table.Column<string>(nullable: true),
                    FeeAddress = table.Column<string>(nullable: true),
                    Tier = table.Column<int>(nullable: false),
                    Signature = table.Column<string>(nullable: true),
                    DateAdded = table.Column<DateTime>(nullable: false),
                    LastSeen = table.Column<DateTime>(nullable: false),
                    Priority = table.Column<long>(nullable: false),
                    Active = table.Column<bool>(nullable: false),
                    Relayed = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_servernode", x => x.ProfileName);
                });

            migrationBuilder.CreateIndex(
                name: "IX_profile_KeyAddress",
                table: "profile",
                column: "KeyAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_profile_Name",
                table: "profile",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_server_ProfileName",
                table: "server",
                column: "ProfileName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_servernode_ProfileName",
                table: "servernode",
                column: "ProfileName",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pricelock");

            migrationBuilder.DropTable(
                name: "profile");

            migrationBuilder.DropTable(
                name: "server");

            migrationBuilder.DropTable(
                name: "servernode");
        }
    }
}
