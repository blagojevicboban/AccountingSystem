using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingData.Migrations
{
    /// <inheritdoc />
    public partial class DodajLegacyDbfKolone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PromenaKod",
                table: "StavkeNaloga",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StariKonto",
                table: "StavkeNaloga",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mesto",
                table: "Konta",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StariKonto",
                table: "Konta",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefon",
                table: "Konta",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ulica",
                table: "Konta",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZiroRacun",
                table: "Konta",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KlasifikacionaSifra",
                table: "Artikli",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Selektovan",
                table: "Artikli",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PromenaKod",
                table: "StavkeNaloga");

            migrationBuilder.DropColumn(
                name: "StariKonto",
                table: "StavkeNaloga");

            migrationBuilder.DropColumn(
                name: "Mesto",
                table: "Konta");

            migrationBuilder.DropColumn(
                name: "StariKonto",
                table: "Konta");

            migrationBuilder.DropColumn(
                name: "Telefon",
                table: "Konta");

            migrationBuilder.DropColumn(
                name: "Ulica",
                table: "Konta");

            migrationBuilder.DropColumn(
                name: "ZiroRacun",
                table: "Konta");

            migrationBuilder.DropColumn(
                name: "KlasifikacionaSifra",
                table: "Artikli");

            migrationBuilder.DropColumn(
                name: "Selektovan",
                table: "Artikli");
        }
    }
}
