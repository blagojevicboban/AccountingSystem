using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiFinansijeData.Migrations
{
    /// <inheritdoc />
    public partial class RazdvojiArtikleIMaterijale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Materijali",
                columns: table => new
                {
                    MaterijalId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SifraArtikla = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    JedinicaMere = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Pakovanje = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materijali", x => x.MaterijalId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Materijali_SifraArtikla",
                table: "Materijali",
                column: "SifraArtikla");

            // Roba (ARTIKLI.DBF) i Materijal (M_SIFR.DBF) su nezavisne legacy šifarničke serije koje mogu
            // deliti istu šifru sa različitim značenjem — ne smeju više deliti istu tabelu/dedup-prostor.
            // Prebaci postojeće Vrsta='Materijal' redove iz Artikli u novu Materijali tabelu pre brisanja kolone.
            migrationBuilder.Sql(@"
                INSERT INTO Materijali (SifraArtikla, Naziv, JedinicaMere, Pakovanje)
                SELECT SifraArtikla, Naziv, JedinicaMere, Pakovanje FROM Artikli WHERE Vrsta = 'Materijal';
            ");
            migrationBuilder.Sql("DELETE FROM Artikli WHERE Vrsta = 'Materijal';");

            migrationBuilder.DropColumn(
                name: "Vrsta",
                table: "Artikli");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Vrsta",
                table: "Artikli",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "Roba");

            migrationBuilder.Sql(@"
                INSERT INTO Artikli (SifraArtikla, Naziv, JedinicaMere, Pakovanje, TarifniBroj, Barkod, NabavnaCena, ProdajnaCena, KlasifikacionaSifra, Selektovan, Vrsta)
                SELECT SifraArtikla, Naziv, JedinicaMere, Pakovanje, NULL, NULL, 0, 0, NULL, 0, 'Materijal' FROM Materijali;
            ");

            migrationBuilder.DropTable(
                name: "Materijali");
        }
    }
}
