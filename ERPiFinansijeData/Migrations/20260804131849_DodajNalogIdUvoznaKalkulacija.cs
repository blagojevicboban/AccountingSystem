using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiFinansijeData.Migrations
{
    /// <inheritdoc />
    public partial class DodajNalogIdUvoznaKalkulacija : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NalogId",
                table: "UvozneKalkulacije",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UvozneKalkulacije_NalogId",
                table: "UvozneKalkulacije",
                column: "NalogId");

            migrationBuilder.AddForeignKey(
                name: "FK_UvozneKalkulacije_Nalozi_NalogId",
                table: "UvozneKalkulacije",
                column: "NalogId",
                principalTable: "Nalozi",
                principalColumn: "NalogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UvozneKalkulacije_Nalozi_NalogId",
                table: "UvozneKalkulacije");

            migrationBuilder.DropIndex(
                name: "IX_UvozneKalkulacije_NalogId",
                table: "UvozneKalkulacije");

            migrationBuilder.DropColumn(
                name: "NalogId",
                table: "UvozneKalkulacije");
        }
    }
}
