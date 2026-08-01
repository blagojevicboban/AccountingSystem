using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingData.Migrations
{
    /// <inheritdoc />
    public partial class DodajDeviznoIUvozneKalkulacije : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DevizniDuguje",
                table: "StavkeNaloga",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DevizniPotrazuje",
                table: "StavkeNaloga",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "KursValute",
                table: "StavkeNaloga",
                type: "decimal(18, 4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Valuta",
                table: "StavkeNaloga",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "UvozneKalkulacije",
                columns: table => new
                {
                    UvoznaKalkulacijaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojKalkulacije = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DatumKalkulacije = table.Column<DateTime>(type: "TEXT", nullable: false),
                    InoPartnerId = table.Column<int>(type: "INTEGER", nullable: false),
                    InoBrojFakture = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DatumInoFakture = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Valuta = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    KursValute = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    UkupnoDevize = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UkupnoFakturaRsd = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    CarinaRsd = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    SpedicijaRsd = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    PrevozRsd = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    OstaliZavisniTroskoviRsd = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UkupnaNabavnaVrednostRsd = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    MagacinId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsKnjizeno = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UvozneKalkulacije", x => x.UvoznaKalkulacijaId);
                    table.ForeignKey(
                        name: "FK_UvozneKalkulacije_Magacini_MagacinId",
                        column: x => x.MagacinId,
                        principalTable: "Magacini",
                        principalColumn: "MagacinId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UvozneKalkulacije_Partneri_InoPartnerId",
                        column: x => x.InoPartnerId,
                        principalTable: "Partneri",
                        principalColumn: "PartnerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UvozneStavke",
                columns: table => new
                {
                    UvoznaStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UvoznaKalkulacijaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ArtikalId = table.Column<int>(type: "INTEGER", nullable: false),
                    Kolicina = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    InoCenaDevize = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    InoIznosDevize = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    InoIznosRsd = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    CarinaProcenat = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    CarinaIznosRsd = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    RasporedjeniZavisniTroskoviRsd = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UkupnaNabavnaVrednostRsd = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    NabavnaCenaPoJediniciRsd = table.Column<decimal>(type: "decimal(18, 4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UvozneStavke", x => x.UvoznaStavkaId);
                    table.ForeignKey(
                        name: "FK_UvozneStavke_Artikli_ArtikalId",
                        column: x => x.ArtikalId,
                        principalTable: "Artikli",
                        principalColumn: "ArtikalId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UvozneStavke_UvozneKalkulacije_UvoznaKalkulacijaId",
                        column: x => x.UvoznaKalkulacijaId,
                        principalTable: "UvozneKalkulacije",
                        principalColumn: "UvoznaKalkulacijaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UvozneKalkulacije_InoPartnerId",
                table: "UvozneKalkulacije",
                column: "InoPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_UvozneKalkulacije_MagacinId",
                table: "UvozneKalkulacije",
                column: "MagacinId");

            migrationBuilder.CreateIndex(
                name: "IX_UvozneStavke_ArtikalId",
                table: "UvozneStavke",
                column: "ArtikalId");

            migrationBuilder.CreateIndex(
                name: "IX_UvozneStavke_UvoznaKalkulacijaId",
                table: "UvozneStavke",
                column: "UvoznaKalkulacijaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UvozneStavke");

            migrationBuilder.DropTable(
                name: "UvozneKalkulacije");

            migrationBuilder.DropColumn(
                name: "DevizniDuguje",
                table: "StavkeNaloga");

            migrationBuilder.DropColumn(
                name: "DevizniPotrazuje",
                table: "StavkeNaloga");

            migrationBuilder.DropColumn(
                name: "KursValute",
                table: "StavkeNaloga");

            migrationBuilder.DropColumn(
                name: "Valuta",
                table: "StavkeNaloga");
        }
    }
}
