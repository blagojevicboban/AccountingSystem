using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiFinansijeData.Migrations
{
    /// <inheritdoc />
    public partial class MapirajPoljaRacunOtpremnice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BrojOtpremnice",
                table: "RacuniOtpremnice",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KontoKupca",
                table: "RacuniOtpremnice",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NacinPlacanja",
                table: "RacuniOtpremnice",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RokPlacanjaDana",
                table: "RacuniOtpremnice",
                type: "INTEGER",
                nullable: false,
                defaultValue: 15);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BrojOtpremnice",
                table: "RacuniOtpremnice");

            migrationBuilder.DropColumn(
                name: "KontoKupca",
                table: "RacuniOtpremnice");

            migrationBuilder.DropColumn(
                name: "NacinPlacanja",
                table: "RacuniOtpremnice");

            migrationBuilder.DropColumn(
                name: "RokPlacanjaDana",
                table: "RacuniOtpremnice");
        }
    }
}
