using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiFinansijeData.Migrations
{
    /// <inheritdoc />
    public partial class DodajStavkeMaloprodajnojKalkulaciji : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaloprodajnaKalkulacijaStavke",
                columns: table => new
                {
                    MaloprodajnaKalkulacijaStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MaloprodajnaKalkulacijaId = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_MaloprodajnaKalkulacijaStavke", x => x.MaloprodajnaKalkulacijaStavkaId);
                    table.ForeignKey(
                        name: "FK_MaloprodajnaKalkulacijaStavke_MaloprodajneKalkulacije_MaloprodajnaKalkulacijaId",
                        column: x => x.MaloprodajnaKalkulacijaId,
                        principalTable: "MaloprodajneKalkulacije",
                        principalColumn: "MaloprodajnaKalkulacijaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaloprodajnaKalkulacijaStavke_MaloprodajnaKalkulacijaId",
                table: "MaloprodajnaKalkulacijaStavke",
                column: "MaloprodajnaKalkulacijaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaloprodajnaKalkulacijaStavke");
        }
    }
}
