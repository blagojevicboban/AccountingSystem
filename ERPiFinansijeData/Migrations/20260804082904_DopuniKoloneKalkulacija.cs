using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiFinansijeData.Migrations
{
    /// <inheritdoc />
    public partial class DopuniKoloneKalkulacija : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Kolicina",
                table: "MaloprodajnaKalkulacijaStavke",
                type: "decimal(18, 4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18, 2)");

            migrationBuilder.AddColumn<int>(
                name: "BrojRazduzenja",
                table: "MaloprodajnaKalkulacijaStavke",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsKnjizen",
                table: "MaloprodajnaKalkulacijaStavke",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTrgovinskiKnjizen",
                table: "MaloprodajnaKalkulacijaStavke",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PorezProcenat",
                table: "MaloprodajnaKalkulacijaStavke",
                type: "decimal(9, 4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PorezZaUplatu",
                table: "MaloprodajnaKalkulacijaStavke",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PosebanPorezIznos",
                table: "MaloprodajnaKalkulacijaStavke",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PosebanPorezProcenat",
                table: "MaloprodajnaKalkulacijaStavke",
                type: "decimal(9, 4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrenetiPorez",
                table: "MaloprodajnaKalkulacijaStavke",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrenetiPosebanPorez",
                table: "MaloprodajnaKalkulacijaStavke",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ProdajnaVrednostBezPoreza",
                table: "MaloprodajnaKalkulacijaStavke",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RazlikaProcenat",
                table: "MaloprodajnaKalkulacijaStavke",
                type: "decimal(18, 6)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Taksa",
                table: "MaloprodajnaKalkulacijaStavke",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TarifniBroj",
                table: "MaloprodajnaKalkulacijaStavke",
                type: "TEXT",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Kolicina",
                table: "KalkulacijaStavke",
                type: "decimal(18, 4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18, 2)");

            migrationBuilder.AddColumn<bool>(
                name: "IsKnjizen",
                table: "KalkulacijaStavke",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PorezProcenat",
                table: "KalkulacijaStavke",
                type: "decimal(9, 4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PorezZaUplatu",
                table: "KalkulacijaStavke",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PosebanPorezIznos",
                table: "KalkulacijaStavke",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PosebanPorezProcenat",
                table: "KalkulacijaStavke",
                type: "decimal(9, 4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrenetiPorez",
                table: "KalkulacijaStavke",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrenetiPosebanPorez",
                table: "KalkulacijaStavke",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ProdajnaVrednostBezPoreza",
                table: "KalkulacijaStavke",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RazlikaProcenat",
                table: "KalkulacijaStavke",
                type: "decimal(18, 6)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StaraCena",
                table: "KalkulacijaStavke",
                type: "decimal(18, 4)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BrojRazduzenja",
                table: "MaloprodajnaKalkulacijaStavke");

            migrationBuilder.DropColumn(
                name: "IsKnjizen",
                table: "MaloprodajnaKalkulacijaStavke");

            migrationBuilder.DropColumn(
                name: "IsTrgovinskiKnjizen",
                table: "MaloprodajnaKalkulacijaStavke");

            migrationBuilder.DropColumn(
                name: "PorezProcenat",
                table: "MaloprodajnaKalkulacijaStavke");

            migrationBuilder.DropColumn(
                name: "PorezZaUplatu",
                table: "MaloprodajnaKalkulacijaStavke");

            migrationBuilder.DropColumn(
                name: "PosebanPorezIznos",
                table: "MaloprodajnaKalkulacijaStavke");

            migrationBuilder.DropColumn(
                name: "PosebanPorezProcenat",
                table: "MaloprodajnaKalkulacijaStavke");

            migrationBuilder.DropColumn(
                name: "PrenetiPorez",
                table: "MaloprodajnaKalkulacijaStavke");

            migrationBuilder.DropColumn(
                name: "PrenetiPosebanPorez",
                table: "MaloprodajnaKalkulacijaStavke");

            migrationBuilder.DropColumn(
                name: "ProdajnaVrednostBezPoreza",
                table: "MaloprodajnaKalkulacijaStavke");

            migrationBuilder.DropColumn(
                name: "RazlikaProcenat",
                table: "MaloprodajnaKalkulacijaStavke");

            migrationBuilder.DropColumn(
                name: "Taksa",
                table: "MaloprodajnaKalkulacijaStavke");

            migrationBuilder.DropColumn(
                name: "TarifniBroj",
                table: "MaloprodajnaKalkulacijaStavke");

            migrationBuilder.DropColumn(
                name: "IsKnjizen",
                table: "KalkulacijaStavke");

            migrationBuilder.DropColumn(
                name: "PorezProcenat",
                table: "KalkulacijaStavke");

            migrationBuilder.DropColumn(
                name: "PorezZaUplatu",
                table: "KalkulacijaStavke");

            migrationBuilder.DropColumn(
                name: "PosebanPorezIznos",
                table: "KalkulacijaStavke");

            migrationBuilder.DropColumn(
                name: "PosebanPorezProcenat",
                table: "KalkulacijaStavke");

            migrationBuilder.DropColumn(
                name: "PrenetiPorez",
                table: "KalkulacijaStavke");

            migrationBuilder.DropColumn(
                name: "PrenetiPosebanPorez",
                table: "KalkulacijaStavke");

            migrationBuilder.DropColumn(
                name: "ProdajnaVrednostBezPoreza",
                table: "KalkulacijaStavke");

            migrationBuilder.DropColumn(
                name: "RazlikaProcenat",
                table: "KalkulacijaStavke");

            migrationBuilder.DropColumn(
                name: "StaraCena",
                table: "KalkulacijaStavke");

            migrationBuilder.AlterColumn<decimal>(
                name: "Kolicina",
                table: "MaloprodajnaKalkulacijaStavke",
                type: "decimal(18, 2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18, 4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Kolicina",
                table: "KalkulacijaStavke",
                type: "decimal(18, 2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18, 4)");
        }
    }
}
