using Microsoft.EntityFrameworkCore.Migrations;

namespace x42.Migrations
{
    public partial class AddDomainTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "domain",
                columns: table => new
                {
                    Name = table.Column<string>(nullable: false),
                    KeyAddress = table.Column<string>(nullable: true),
                    ReturnAddress = table.Column<string>(nullable: true),
                    Signature = table.Column<string>(nullable: true),
                    Status = table.Column<int>(nullable: false),
                    PriceLockId = table.Column<string>(nullable: true),
                    BlockConfirmed = table.Column<int>(nullable: false),
                    Relayed = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_domain", x => x.Name);
                });

            migrationBuilder.CreateIndex(
                name: "IX_domain_BlockConfirmed",
                table: "domain",
                column: "BlockConfirmed");

            migrationBuilder.CreateIndex(
                name: "IX_domain_KeyAddress",
                table: "domain",
                column: "KeyAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_domain_Name",
                table: "domain",
                column: "Name",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "domain");
        }
    }
}
