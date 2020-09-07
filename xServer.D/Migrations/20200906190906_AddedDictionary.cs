using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace x42.Migrations
{
    public partial class AddedDictionary : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "server");

            migrationBuilder.CreateTable(
                name: "dictionary",
                columns: table => new
                {
                    Key = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dictionary", x => x.Key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dictionary_Key",
                table: "dictionary",
                column: "Key",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dictionary");

            migrationBuilder.CreateTable(
                name: "server",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ProfileHeight = table.Column<int>(type: "integer", nullable: false),
                    ProfileName = table.Column<string>(type: "text", nullable: true),
                    SignAddress = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_server_ProfileName",
                table: "server",
                column: "ProfileName",
                unique: true);
        }
    }
}
