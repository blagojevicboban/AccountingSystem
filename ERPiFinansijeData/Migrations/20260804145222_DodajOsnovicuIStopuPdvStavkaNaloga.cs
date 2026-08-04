using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiFinansijeData.Migrations
{
    /// <inheritdoc />
    public partial class DodajOsnovicuIStopuPdvStavkaNaloga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Osnovica",
                table: "StavkeNaloga",
                type: "decimal(18, 2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StopaPdv",
                table: "StavkeNaloga",
                type: "decimal(18, 2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Osnovica",
                table: "StavkeNaloga");

            migrationBuilder.DropColumn(
                name: "StopaPdv",
                table: "StavkeNaloga");
        }
    }
}
