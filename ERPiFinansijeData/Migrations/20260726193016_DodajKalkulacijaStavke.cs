using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiFinansijeData.Migrations
{
    /// <inheritdoc />
    public partial class DodajKalkulacijaStavke : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KalkulacijaStavke",
                columns: table => new
                {
                    KalkulacijaStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KalkulacijaId = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    SifraArtikla = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Kolicina = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    NabavnaCena = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    Iznos = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Troskovi = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    NabavnaVrednost = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    RazlikaIznos = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    PorezIznos = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    ProdajnaVrednost = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    ProdajnaCena = table.Column<decimal>(type: "decimal(18, 4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KalkulacijaStavke", x => x.KalkulacijaStavkaId);
                    table.ForeignKey(
                        name: "FK_KalkulacijaStavke_Kalkulacije_KalkulacijaId",
                        column: x => x.KalkulacijaId,
                        principalTable: "Kalkulacije",
                        principalColumn: "KalkulacijaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Korisnici",
                keyColumn: "KorisnikId",
                keyValue: 1,
                column: "LozinkaHash",
                value: "PBKDF2$100000$IxpGjzsTHvV0x7fZq6RdJQ==$6ERduoiJeJ9Iwc5bF56gYD0r3MqcFCWBYyw8XTHQ3u4=");

            migrationBuilder.CreateIndex(
                name: "IX_KalkulacijaStavke_KalkulacijaId",
                table: "KalkulacijaStavke",
                column: "KalkulacijaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KalkulacijaStavke");

            migrationBuilder.UpdateData(
                table: "Korisnici",
                keyColumn: "KorisnikId",
                keyValue: 1,
                column: "LozinkaHash",
                value: "PBKDF2$100000$CnYWiALqycqWTueq6ayEvQ==$hvm9e8z3e+KVeRsego3azOuoTp3q64deikPgUB9/D4o=");
        }
    }
}
