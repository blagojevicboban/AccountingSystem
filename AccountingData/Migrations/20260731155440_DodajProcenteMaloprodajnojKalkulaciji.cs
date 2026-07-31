using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingData.Migrations
{
    /// <inheritdoc />
    public partial class DodajProcenteMaloprodajnojKalkulaciji : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MarzaProcenat",
                table: "MaloprodajneKalkulacije",
                type: "decimal(9, 4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PoreskaStopaProcenat",
                table: "MaloprodajneKalkulacije",
                type: "decimal(9, 4)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MarzaProcenat",
                table: "MaloprodajneKalkulacije");

            migrationBuilder.DropColumn(
                name: "PoreskaStopaProcenat",
                table: "MaloprodajneKalkulacije");
        }
    }
}
