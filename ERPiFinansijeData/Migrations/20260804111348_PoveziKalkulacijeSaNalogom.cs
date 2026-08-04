using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiFinansijeData.Migrations
{
    /// <inheritdoc />
    public partial class PoveziKalkulacijeSaNalogom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NalogId",
                table: "MaloprodajneKalkulacije",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NalogId",
                table: "Kalkulacije",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaloprodajneKalkulacije_NalogId",
                table: "MaloprodajneKalkulacije",
                column: "NalogId");

            migrationBuilder.CreateIndex(
                name: "IX_Kalkulacije_NalogId",
                table: "Kalkulacije",
                column: "NalogId");

            migrationBuilder.AddForeignKey(
                name: "FK_Kalkulacije_Nalozi_NalogId",
                table: "Kalkulacije",
                column: "NalogId",
                principalTable: "Nalozi",
                principalColumn: "NalogId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaloprodajneKalkulacije_Nalozi_NalogId",
                table: "MaloprodajneKalkulacije",
                column: "NalogId",
                principalTable: "Nalozi",
                principalColumn: "NalogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kalkulacije_Nalozi_NalogId",
                table: "Kalkulacije");

            migrationBuilder.DropForeignKey(
                name: "FK_MaloprodajneKalkulacije_Nalozi_NalogId",
                table: "MaloprodajneKalkulacije");

            migrationBuilder.DropIndex(
                name: "IX_MaloprodajneKalkulacije_NalogId",
                table: "MaloprodajneKalkulacije");

            migrationBuilder.DropIndex(
                name: "IX_Kalkulacije_NalogId",
                table: "Kalkulacije");

            migrationBuilder.DropColumn(
                name: "NalogId",
                table: "MaloprodajneKalkulacije");

            migrationBuilder.DropColumn(
                name: "NalogId",
                table: "Kalkulacije");
        }
    }
}
