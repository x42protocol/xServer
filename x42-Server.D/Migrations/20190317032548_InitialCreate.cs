using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace x42.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "masternode",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Ip = table.Column<string>(nullable: true),
                    Port = table.Column<string>(nullable: true),
                    Address = table.Column<string>(nullable: true),
                    Signature = table.Column<string>(nullable: true),
                    DateAdded = table.Column<DateTime>(nullable: false),
                    LastSeen = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_masternode", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "masternode");
        }
    }
}
