using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingData.Migrations
{
    /// <inheritdoc />
    public partial class AddRacunOtpremnicaAndNivelacija : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NivelacijeCena",
                columns: table => new
                {
                    NivelacijaCenaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojNivelacije = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    DatumNivelacije = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MagacinId = table.Column<int>(type: "INTEGER", nullable: false),
                    Opis = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    UkupnoRazlika = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    IsKnjizen = table.Column<bool>(type: "INTEGER", nullable: false),
                    NalogId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NivelacijeCena", x => x.NivelacijaCenaId);
                    table.ForeignKey(
                        name: "FK_NivelacijeCena_Magacini_MagacinId",
                        column: x => x.MagacinId,
                        principalTable: "Magacini",
                        principalColumn: "MagacinId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NivelacijeCena_Nalozi_NalogId",
                        column: x => x.NalogId,
                        principalTable: "Nalozi",
                        principalColumn: "NalogId");
                });

            migrationBuilder.CreateTable(
                name: "RacuniOtpremnice",
                columns: table => new
                {
                    RacunOtpremnicaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojRacuna = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    DatumRacuna = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RokPlacanja = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: true),
                    MagacinId = table.Column<int>(type: "INTEGER", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    UkupnoOsnovica = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UkupnoRabat = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UkupnoPdv = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UkupnoZaUplatu = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    IsKnjizen = table.Column<bool>(type: "INTEGER", nullable: false),
                    NalogId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RacuniOtpremnice", x => x.RacunOtpremnicaId);
                    table.ForeignKey(
                        name: "FK_RacuniOtpremnice_Magacini_MagacinId",
                        column: x => x.MagacinId,
                        principalTable: "Magacini",
                        principalColumn: "MagacinId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RacuniOtpremnice_Nalozi_NalogId",
                        column: x => x.NalogId,
                        principalTable: "Nalozi",
                        principalColumn: "NalogId");
                    table.ForeignKey(
                        name: "FK_RacuniOtpremnice_Partneri_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partneri",
                        principalColumn: "PartnerId");
                });

            migrationBuilder.CreateTable(
                name: "NivelacijaStavke",
                columns: table => new
                {
                    NivelacijaStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NivelacijaCenaId = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    ArtikalId = table.Column<int>(type: "INTEGER", nullable: false),
                    KolicinaZaliha = table.Column<decimal>(type: "decimal(18, 3)", nullable: false),
                    StaraCena = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    NovaCena = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    RazlikaPoJedinici = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UkupnaRazlika = table.Column<decimal>(type: "decimal(18, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NivelacijaStavke", x => x.NivelacijaStavkaId);
                    table.ForeignKey(
                        name: "FK_NivelacijaStavke_Artikli_ArtikalId",
                        column: x => x.ArtikalId,
                        principalTable: "Artikli",
                        principalColumn: "ArtikalId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NivelacijaStavke_NivelacijeCena_NivelacijaCenaId",
                        column: x => x.NivelacijaCenaId,
                        principalTable: "NivelacijeCena",
                        principalColumn: "NivelacijaCenaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RacunOtpremnicaStavke",
                columns: table => new
                {
                    RacunOtpremnicaStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RacunOtpremnicaId = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    ArtikalId = table.Column<int>(type: "INTEGER", nullable: false),
                    Kolicina = table.Column<decimal>(type: "decimal(18, 3)", nullable: false),
                    ProdajnaCena = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    RabatProcenat = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    StopaPdv = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Osnovica = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    IznosPdv = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Ukupno = table.Column<decimal>(type: "decimal(18, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RacunOtpremnicaStavke", x => x.RacunOtpremnicaStavkaId);
                    table.ForeignKey(
                        name: "FK_RacunOtpremnicaStavke_Artikli_ArtikalId",
                        column: x => x.ArtikalId,
                        principalTable: "Artikli",
                        principalColumn: "ArtikalId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RacunOtpremnicaStavke_RacuniOtpremnice_RacunOtpremnicaId",
                        column: x => x.RacunOtpremnicaId,
                        principalTable: "RacuniOtpremnice",
                        principalColumn: "RacunOtpremnicaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NivelacijaStavke_ArtikalId",
                table: "NivelacijaStavke",
                column: "ArtikalId");

            migrationBuilder.CreateIndex(
                name: "IX_NivelacijaStavke_NivelacijaCenaId",
                table: "NivelacijaStavke",
                column: "NivelacijaCenaId");

            migrationBuilder.CreateIndex(
                name: "IX_NivelacijeCena_MagacinId",
                table: "NivelacijeCena",
                column: "MagacinId");

            migrationBuilder.CreateIndex(
                name: "IX_NivelacijeCena_NalogId",
                table: "NivelacijeCena",
                column: "NalogId");

            migrationBuilder.CreateIndex(
                name: "IX_RacuniOtpremnice_MagacinId",
                table: "RacuniOtpremnice",
                column: "MagacinId");

            migrationBuilder.CreateIndex(
                name: "IX_RacuniOtpremnice_NalogId",
                table: "RacuniOtpremnice",
                column: "NalogId");

            migrationBuilder.CreateIndex(
                name: "IX_RacuniOtpremnice_PartnerId",
                table: "RacuniOtpremnice",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_RacunOtpremnicaStavke_ArtikalId",
                table: "RacunOtpremnicaStavke",
                column: "ArtikalId");

            migrationBuilder.CreateIndex(
                name: "IX_RacunOtpremnicaStavke_RacunOtpremnicaId",
                table: "RacunOtpremnicaStavke",
                column: "RacunOtpremnicaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NivelacijaStavke");

            migrationBuilder.DropTable(
                name: "RacunOtpremnicaStavke");

            migrationBuilder.DropTable(
                name: "NivelacijeCena");

            migrationBuilder.DropTable(
                name: "RacuniOtpremnice");
        }
    }
}
