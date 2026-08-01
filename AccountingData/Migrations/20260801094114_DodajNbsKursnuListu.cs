using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingData.Migrations
{
    /// <inheritdoc />
    public partial class DodajNbsKursnuListu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KursneListeStavke",
                columns: table => new
                {
                    KursnaListaStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ValutaOznaka = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    ValutaSifra = table.Column<int>(type: "INTEGER", nullable: false),
                    NazivValute = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Jedinica = table.Column<int>(type: "INTEGER", nullable: false),
                    SrednjiKurs = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    KupovniKurs = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    ProdavniKurs = table.Column<decimal>(type: "decimal(18, 4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KursneListeStavke", x => x.KursnaListaStavkaId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KursneListeStavke");
        }
    }
}
