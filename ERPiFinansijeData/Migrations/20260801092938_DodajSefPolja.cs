using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiFinansijeData.Migrations
{
    /// <inheritdoc />
    public partial class DodajSefPolja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SefDatumSlanja",
                table: "RacuniOtpremnice",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SefId",
                table: "RacuniOtpremnice",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SefPoruka",
                table: "RacuniOtpremnice",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SefStatus",
                table: "RacuniOtpremnice",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Firme",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JbkjsBroj",
                table: "Firme",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SefApiKey",
                table: "Firme",
                type: "TEXT",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SefEnvironment",
                table: "Firme",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SefDatumSlanja",
                table: "RacuniOtpremnice");

            migrationBuilder.DropColumn(
                name: "SefId",
                table: "RacuniOtpremnice");

            migrationBuilder.DropColumn(
                name: "SefPoruka",
                table: "RacuniOtpremnice");

            migrationBuilder.DropColumn(
                name: "SefStatus",
                table: "RacuniOtpremnice");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Firme");

            migrationBuilder.DropColumn(
                name: "JbkjsBroj",
                table: "Firme");

            migrationBuilder.DropColumn(
                name: "SefApiKey",
                table: "Firme");

            migrationBuilder.DropColumn(
                name: "SefEnvironment",
                table: "Firme");
        }
    }
}
