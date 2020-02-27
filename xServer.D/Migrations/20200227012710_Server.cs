using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace x42.Migrations
{
    public partial class Server : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_server",
                table: "server");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "server");

            migrationBuilder.DropColumn(
                name: "CAddress",
                table: "server");

            migrationBuilder.DropColumn(
                name: "HAddress",
                table: "server");

            migrationBuilder.DropColumn(
                name: "Ip",
                table: "server");

            migrationBuilder.DropColumn(
                name: "Port",
                table: "server");

            migrationBuilder.DropColumn(
                name: "Signature",
                table: "server");

            migrationBuilder.AddColumn<string>(
                name: "PublicAddress",
                table: "server",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_server",
                table: "server",
                column: "PublicAddress");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_server",
                table: "server");

            migrationBuilder.DropColumn(
                name: "PublicAddress",
                table: "server");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "server",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "CAddress",
                table: "server",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HAddress",
                table: "server",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ip",
                table: "server",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Port",
                table: "server",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Signature",
                table: "server",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_server",
                table: "server",
                column: "Id");
        }
    }
}
