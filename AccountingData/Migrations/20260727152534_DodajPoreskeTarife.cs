using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingData.Migrations
{
    /// <inheritdoc />
    public partial class DodajPoreskeTarife : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NivelacijaStavke_Artikli_ArtikalId",
                table: "NivelacijaStavke");

            migrationBuilder.DropForeignKey(
                name: "FK_NivelacijeCena_Magacini_MagacinId",
                table: "NivelacijeCena");

            migrationBuilder.DropForeignKey(
                name: "FK_RacuniOtpremnice_Magacini_MagacinId",
                table: "RacuniOtpremnice");

            migrationBuilder.DropForeignKey(
                name: "FK_RacunOtpremnicaStavke_Artikli_ArtikalId",
                table: "RacunOtpremnicaStavke");

            migrationBuilder.AlterColumn<int>(
                name: "MagacinId",
                table: "NivelacijeCena",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "ArtikalId",
                table: "NivelacijaStavke",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.CreateTable(
                name: "PoreskeTarife",
                columns: table => new
                {
                    PoreskaTarifaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TarifniBroj = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    PorezProcenat = table.Column<decimal>(type: "decimal(5, 2)", nullable: false),
                    PosebanPorezProcenat = table.Column<decimal>(type: "decimal(5, 2)", nullable: false),
                    PorezUCeni = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoreskeTarife", x => x.PoreskaTarifaId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PoreskeTarife_TarifniBroj",
                table: "PoreskeTarife",
                column: "TarifniBroj",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_NivelacijaStavke_Artikli_ArtikalId",
                table: "NivelacijaStavke",
                column: "ArtikalId",
                principalTable: "Artikli",
                principalColumn: "ArtikalId");

            migrationBuilder.AddForeignKey(
                name: "FK_NivelacijeCena_Magacini_MagacinId",
                table: "NivelacijeCena",
                column: "MagacinId",
                principalTable: "Magacini",
                principalColumn: "MagacinId");

            migrationBuilder.AddForeignKey(
                name: "FK_RacuniOtpremnice_Magacini_MagacinId",
                table: "RacuniOtpremnice",
                column: "MagacinId",
                principalTable: "Magacini",
                principalColumn: "MagacinId");

            migrationBuilder.AddForeignKey(
                name: "FK_RacunOtpremnicaStavke_Artikli_ArtikalId",
                table: "RacunOtpremnicaStavke",
                column: "ArtikalId",
                principalTable: "Artikli",
                principalColumn: "ArtikalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NivelacijaStavke_Artikli_ArtikalId",
                table: "NivelacijaStavke");

            migrationBuilder.DropForeignKey(
                name: "FK_NivelacijeCena_Magacini_MagacinId",
                table: "NivelacijeCena");

            migrationBuilder.DropForeignKey(
                name: "FK_RacuniOtpremnice_Magacini_MagacinId",
                table: "RacuniOtpremnice");

            migrationBuilder.DropForeignKey(
                name: "FK_RacunOtpremnicaStavke_Artikli_ArtikalId",
                table: "RacunOtpremnicaStavke");

            migrationBuilder.DropTable(
                name: "PoreskeTarife");

            migrationBuilder.AlterColumn<int>(
                name: "MagacinId",
                table: "NivelacijeCena",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ArtikalId",
                table: "NivelacijaStavke",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_NivelacijaStavke_Artikli_ArtikalId",
                table: "NivelacijaStavke",
                column: "ArtikalId",
                principalTable: "Artikli",
                principalColumn: "ArtikalId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NivelacijeCena_Magacini_MagacinId",
                table: "NivelacijeCena",
                column: "MagacinId",
                principalTable: "Magacini",
                principalColumn: "MagacinId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RacuniOtpremnice_Magacini_MagacinId",
                table: "RacuniOtpremnice",
                column: "MagacinId",
                principalTable: "Magacini",
                principalColumn: "MagacinId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RacunOtpremnicaStavke_Artikli_ArtikalId",
                table: "RacunOtpremnicaStavke",
                column: "ArtikalId",
                principalTable: "Artikli",
                principalColumn: "ArtikalId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
