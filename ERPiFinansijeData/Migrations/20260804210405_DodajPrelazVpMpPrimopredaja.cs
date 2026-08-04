using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiFinansijeData.Migrations
{
    /// <inheritdoc />
    public partial class DodajPrelazVpMpPrimopredaja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NalogId",
                table: "PrimopredajaNalozi",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StopaPdv",
                table: "PrimopredajaNalozi",
                type: "decimal(5, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_PrimopredajaNalozi_NalogId",
                table: "PrimopredajaNalozi",
                column: "NalogId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrimopredajaNalozi_Nalozi_NalogId",
                table: "PrimopredajaNalozi",
                column: "NalogId",
                principalTable: "Nalozi",
                principalColumn: "NalogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrimopredajaNalozi_Nalozi_NalogId",
                table: "PrimopredajaNalozi");

            migrationBuilder.DropIndex(
                name: "IX_PrimopredajaNalozi_NalogId",
                table: "PrimopredajaNalozi");

            migrationBuilder.DropColumn(
                name: "NalogId",
                table: "PrimopredajaNalozi");

            migrationBuilder.DropColumn(
                name: "StopaPdv",
                table: "PrimopredajaNalozi");
        }
    }
}
