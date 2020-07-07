using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace x42.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "profile",
                columns: table => new
                {
                    KeyAddress = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Signature = table.Column<string>(nullable: true),
                    TransactionId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile", x => x.KeyAddress);
                });

            migrationBuilder.CreateTable(
                name: "server",
                columns: table => new
                {
                    KeyAddress = table.Column<string>(nullable: false),
                    PublicAddress = table.Column<string>(nullable: true),
                    DateAdded = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server", x => x.KeyAddress);
                });

            migrationBuilder.CreateTable(
                name: "servernode",
                columns: table => new
                {
                    KeyAddress = table.Column<string>(nullable: false),
                    NetworkProtocol = table.Column<int>(nullable: false),
                    NetworkAddress = table.Column<string>(nullable: true),
                    NetworkPort = table.Column<long>(nullable: false),
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
                    table.PrimaryKey("PK_servernode", x => x.KeyAddress);
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
                name: "IX_server_KeyAddress",
                table: "server",
                column: "KeyAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_servernode_KeyAddress",
                table: "servernode",
                column: "KeyAddress",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "profile");

            migrationBuilder.DropTable(
                name: "server");

            migrationBuilder.DropTable(
                name: "servernode");
        }
    }
}
