using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiFinansijeData.Migrations
{
    /// <inheritdoc />
    public partial class DodajKontoPartneraKompenzacija : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KontoPartnera1",
                table: "Kompenzacije",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KontoPartnera2",
                table: "Kompenzacije",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KontoPartnera3",
                table: "Kompenzacije",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KontoPartnera1",
                table: "Kompenzacije");

            migrationBuilder.DropColumn(
                name: "KontoPartnera2",
                table: "Kompenzacije");

            migrationBuilder.DropColumn(
                name: "KontoPartnera3",
                table: "Kompenzacije");
        }
    }
}
