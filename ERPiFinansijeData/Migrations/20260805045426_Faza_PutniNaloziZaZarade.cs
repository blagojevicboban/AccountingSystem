using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiFinansijeData.Migrations
{
    /// <summary>
    /// Faza 3.2 — priprema za prenos oporezivog dela dnevnice u ERPiZarade.
    ///
    /// <c>PutniNalog.Jmbg</c> je novo polje za uparivanje naloga sa tačnim radnikom pri uvozu
    /// na strani zarada (ovaj program nema svoj registar zaposlenih). <c>NeoporeziviIznosiDnevnice</c>
    /// je datumski-efektivan šifarnik zakonskog limita — isti obrazac kao <c>KamatneStope</c> —
    /// pa se prekoračenje ne računa napamet po konstanti u kodu.
    ///
    /// Seed red (2026: 3.471 RSD za zemlju) je potvrđena, važeća vrednost za tekuću godinu —
    /// izuzetak od pravila „propis se ne unosi u kod" jer je ovde reč o šifarniku čiji je
    /// prvi red uslov da nova funkcija uopšte proradi, ne o konstanti u obračunskoj logici.
    /// </summary>
    public partial class Faza_PutniNaloziZaZarade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Jmbg",
                table: "PutniNalozi",
                type: "TEXT",
                maxLength: 13,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "NeoporeziviIznosiDnevnice",
                columns: table => new
                {
                    NeoporeziviIznosDnevniceId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DatumOd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IznosZemljaRsd = table.Column<decimal>(type: "decimal(10, 2)", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NeoporeziviIznosiDnevnice", x => x.NeoporeziviIznosDnevniceId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NeoporeziviIznosiDnevnice_DatumOd",
                table: "NeoporeziviIznosiDnevnice",
                column: "DatumOd");

            migrationBuilder.InsertData(
                table: "NeoporeziviIznosiDnevnice",
                columns: new[] { "DatumOd", "IznosZemljaRsd", "Napomena" },
                values: new object[] { new DateTime(2026, 1, 1), 3471m,
                    "Usklađeni neoporezivi iznos dnevnice za 2026. godinu (čl. 5, tačka 11 Zakona o porezu na dohodak građana)." });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NeoporeziviIznosiDnevnice");

            migrationBuilder.DropColumn(
                name: "Jmbg",
                table: "PutniNalozi");
        }
    }
}
