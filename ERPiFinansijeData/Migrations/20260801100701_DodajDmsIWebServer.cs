using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiFinansijeData.Migrations
{
    /// <inheritdoc />
    public partial class DodajDmsIWebServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DokumentiPrilozi",
                columns: table => new
                {
                    DokumentPrilogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NalogId = table.Column<int>(type: "INTEGER", nullable: true),
                    RacunOtpremnicaId = table.Column<int>(type: "INTEGER", nullable: true),
                    KalkulacijaId = table.Column<int>(type: "INTEGER", nullable: true),
                    NazivFajla = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    TipDokumenta = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PutanjaFajla = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    VelicinaBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    DatumPriloga = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Korisnik = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DokumentiPrilozi", x => x.DokumentPrilogId);
                    table.ForeignKey(
                        name: "FK_DokumentiPrilozi_Kalkulacije_KalkulacijaId",
                        column: x => x.KalkulacijaId,
                        principalTable: "Kalkulacije",
                        principalColumn: "KalkulacijaId");
                    table.ForeignKey(
                        name: "FK_DokumentiPrilozi_Nalozi_NalogId",
                        column: x => x.NalogId,
                        principalTable: "Nalozi",
                        principalColumn: "NalogId");
                    table.ForeignKey(
                        name: "FK_DokumentiPrilozi_RacuniOtpremnice_RacunOtpremnicaId",
                        column: x => x.RacunOtpremnicaId,
                        principalTable: "RacuniOtpremnice",
                        principalColumn: "RacunOtpremnicaId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DokumentiPrilozi_KalkulacijaId",
                table: "DokumentiPrilozi",
                column: "KalkulacijaId");

            migrationBuilder.CreateIndex(
                name: "IX_DokumentiPrilozi_NalogId",
                table: "DokumentiPrilozi",
                column: "NalogId");

            migrationBuilder.CreateIndex(
                name: "IX_DokumentiPrilozi_RacunOtpremnicaId",
                table: "DokumentiPrilozi",
                column: "RacunOtpremnicaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DokumentiPrilozi");
        }
    }
}
