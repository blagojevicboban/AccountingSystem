using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiFinansijeData.Migrations
{
    /// <inheritdoc />
    public partial class DodajZatvaranjeStavki : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ZatvaranjaStavki",
                columns: table => new
                {
                    ZatvaranjeStavkeId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StavkaDugujeId = table.Column<int>(type: "INTEGER", nullable: false),
                    StavkaPotrazujeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Iznos = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    DatumZatvaranja = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VrstaZatvaranja = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    KorisnikId = table.Column<int>(type: "INTEGER", nullable: true),
                    KorisnickoIme = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZatvaranjaStavki", x => x.ZatvaranjeStavkeId);
                    table.ForeignKey(
                        name: "FK_ZatvaranjaStavki_StavkeNaloga_StavkaDugujeId",
                        column: x => x.StavkaDugujeId,
                        principalTable: "StavkeNaloga",
                        principalColumn: "StavkaNalogaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ZatvaranjaStavki_StavkeNaloga_StavkaPotrazujeId",
                        column: x => x.StavkaPotrazujeId,
                        principalTable: "StavkeNaloga",
                        principalColumn: "StavkaNalogaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ZatvaranjaStavki_StavkaDugujeId",
                table: "ZatvaranjaStavki",
                column: "StavkaDugujeId");

            migrationBuilder.CreateIndex(
                name: "IX_ZatvaranjaStavki_StavkaPotrazujeId",
                table: "ZatvaranjaStavki",
                column: "StavkaPotrazujeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZatvaranjaStavki");
        }
    }
}
