using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiFinansijeData.Migrations
{
    /// <inheritdoc />
    public partial class DodajKamatneStope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KamatneStope",
                columns: table => new
                {
                    KamatnaStopaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DatumOd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GodisnjaStopaProcenat = table.Column<decimal>(type: "decimal(9, 4)", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KamatneStope", x => x.KamatnaStopaId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KamatneStope_DatumOd",
                table: "KamatneStope",
                column: "DatumOd");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KamatneStope");
        }
    }
}
