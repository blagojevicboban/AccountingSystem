using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiFinansijeData.Migrations
{
    /// <inheritdoc />
    public partial class DodajEsirFiskalizaciju : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FiskalniBroj",
                table: "RacuniOtpremnice",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FiskalniDatum",
                table: "RacuniOtpremnice",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FiskalniQrKod",
                table: "RacuniOtpremnice",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FiskalniStatus",
                table: "RacuniOtpremnice",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PfrKasirName",
                table: "Firme",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PfrPacKod",
                table: "Firme",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PfrUrl",
                table: "Firme",
                type: "TEXT",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "FiskalniRacuniLog",
                columns: table => new
                {
                    FiskalniRacunLogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RacunOtpremnicaId = table.Column<int>(type: "INTEGER", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    InvoiceCounter = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SdcDateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    InvoiceType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TransactionType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    PaymentType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    QrCodeData = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    VerificationUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Kasir = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RawJsonResponse = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiskalniRacuniLog", x => x.FiskalniRacunLogId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FiskalniRacuniLog");

            migrationBuilder.DropColumn(
                name: "FiskalniBroj",
                table: "RacuniOtpremnice");

            migrationBuilder.DropColumn(
                name: "FiskalniDatum",
                table: "RacuniOtpremnice");

            migrationBuilder.DropColumn(
                name: "FiskalniQrKod",
                table: "RacuniOtpremnice");

            migrationBuilder.DropColumn(
                name: "FiskalniStatus",
                table: "RacuniOtpremnice");

            migrationBuilder.DropColumn(
                name: "PfrKasirName",
                table: "Firme");

            migrationBuilder.DropColumn(
                name: "PfrPacKod",
                table: "Firme");

            migrationBuilder.DropColumn(
                name: "PfrUrl",
                table: "Firme");
        }
    }
}
