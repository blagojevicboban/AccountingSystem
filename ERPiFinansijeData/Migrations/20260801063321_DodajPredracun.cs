using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiFinansijeData.Migrations
{
    /// <inheritdoc />
    public partial class DodajPredracun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RokVazenjaPredracuna",
                table: "RacuniOtpremnice",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipDokumenta",
                table: "RacuniOtpremnice",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RokVazenjaPredracuna",
                table: "RacuniOtpremnice");

            migrationBuilder.DropColumn(
                name: "TipDokumenta",
                table: "RacuniOtpremnice");
        }
    }
}
