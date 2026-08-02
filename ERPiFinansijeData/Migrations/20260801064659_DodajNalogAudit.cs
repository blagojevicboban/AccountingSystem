using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiFinansijeData.Migrations
{
    /// <inheritdoc />
    public partial class DodajNalogAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NalogAuditi",
                columns: table => new
                {
                    NalogAuditId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NalogId = table.Column<int>(type: "INTEGER", nullable: false),
                    BrojNaloga = table.Column<int>(type: "INTEGER", nullable: false),
                    Akcija = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    KorisnikId = table.Column<int>(type: "INTEGER", nullable: true),
                    KorisnickoIme = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Vreme = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NalogAuditi", x => x.NalogAuditId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NalogAuditi_NalogId",
                table: "NalogAuditi",
                column: "NalogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NalogAuditi");
        }
    }
}
