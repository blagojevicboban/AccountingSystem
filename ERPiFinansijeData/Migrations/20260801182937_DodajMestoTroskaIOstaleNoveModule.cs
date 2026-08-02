using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPiFinansijeData.Migrations
{
    /// <inheritdoc />
    public partial class DodajMestoTroskaIOstaleNoveModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MestoTroskaId",
                table: "StavkeNaloga",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BlagajnickiNalozi",
                columns: table => new
                {
                    BlagajnickiNalogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojNaloga = table.Column<string>(type: "TEXT", nullable: false),
                    VrstaBlagajne = table.Column<int>(type: "INTEGER", nullable: false),
                    VrstaNaloga = table.Column<int>(type: "INTEGER", nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UplatilacIsplatilac = table.Column<string>(type: "TEXT", nullable: false),
                    Svrha = table.Column<string>(type: "TEXT", nullable: false),
                    BrojKontaProtu = table.Column<string>(type: "TEXT", nullable: false),
                    Iznos = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", nullable: false),
                    Korisnik = table.Column<string>(type: "TEXT", nullable: false),
                    NalogId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsKnjizeno = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlagajnickiNalozi", x => x.BlagajnickiNalogId);
                });

            migrationBuilder.CreateTable(
                name: "Kompenzacije",
                columns: table => new
                {
                    KompenzacijaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojDokumenta = table.Column<string>(type: "TEXT", nullable: false),
                    Vrsta = table.Column<int>(type: "INTEGER", nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: true),
                    NazivPartnera = table.Column<string>(type: "TEXT", nullable: false),
                    Partner2Id = table.Column<int>(type: "INTEGER", nullable: true),
                    NazivPartnera2 = table.Column<string>(type: "TEXT", nullable: true),
                    Partner3Id = table.Column<int>(type: "INTEGER", nullable: true),
                    NazivPartnera3 = table.Column<string>(type: "TEXT", nullable: true),
                    UkupanIznosKompenzacije = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", nullable: false),
                    Korisnik = table.Column<string>(type: "TEXT", nullable: false),
                    NalogId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsKnjizeno = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kompenzacije", x => x.KompenzacijaId);
                });

            migrationBuilder.CreateTable(
                name: "MestaTroska",
                columns: table => new
                {
                    MestoTroskaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sifra = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Tip = table.Column<int>(type: "INTEGER", nullable: false),
                    IsAktivno = table.Column<bool>(type: "INTEGER", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MestaTroska", x => x.MestoTroskaId);
                });

            migrationBuilder.CreateTable(
                name: "NarudzbeniceDobavljacima",
                columns: table => new
                {
                    NarudzbenicaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojNarudzbenice = table.Column<string>(type: "TEXT", nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RokIsporuke = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: true),
                    NazivDobavljaca = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    UkupnoNeto = table.Column<decimal>(type: "TEXT", nullable: false),
                    UkupnoPdv = table.Column<decimal>(type: "TEXT", nullable: false),
                    UkupnoBruto = table.Column<decimal>(type: "TEXT", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", nullable: false),
                    Korisnik = table.Column<string>(type: "TEXT", nullable: false),
                    KalkulacijaId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NarudzbeniceDobavljacima", x => x.NarudzbenicaId);
                });

            migrationBuilder.CreateTable(
                name: "PonudePredracuni",
                columns: table => new
                {
                    PonudaPredracunId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojDokumenta = table.Column<string>(type: "TEXT", nullable: false),
                    VrstaDokumenta = table.Column<string>(type: "TEXT", nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RokVazenja = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: true),
                    NazivPartnera = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    UkupnoNeto = table.Column<decimal>(type: "TEXT", nullable: false),
                    UkupnoPdv = table.Column<decimal>(type: "TEXT", nullable: false),
                    UkupnoBruto = table.Column<decimal>(type: "TEXT", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", nullable: false),
                    Korisnik = table.Column<string>(type: "TEXT", nullable: false),
                    RacunOtpremnicaId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PonudePredracuni", x => x.PonudaPredracunId);
                });

            migrationBuilder.CreateTable(
                name: "PutniNalozi",
                columns: table => new
                {
                    PutniNalogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojNaloga = table.Column<string>(type: "TEXT", nullable: false),
                    Vrsta = table.Column<int>(type: "INTEGER", nullable: false),
                    ZaposleniIme = table.Column<string>(type: "TEXT", nullable: false),
                    RadnoMesto = table.Column<string>(type: "TEXT", nullable: false),
                    Relacija = table.Column<string>(type: "TEXT", nullable: false),
                    SvrhaPutovanja = table.Column<string>(type: "TEXT", nullable: false),
                    PrevoznoSredstvo = table.Column<string>(type: "TEXT", nullable: false),
                    DatumPolaska = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DatumPovratka = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TrajanjeSati = table.Column<double>(type: "REAL", nullable: false),
                    BrojDnevnica = table.Column<decimal>(type: "TEXT", nullable: false),
                    IznosDnevniceRsd = table.Column<decimal>(type: "TEXT", nullable: false),
                    UkupnoDnevnice = table.Column<decimal>(type: "TEXT", nullable: false),
                    TroskoviGoriva = table.Column<decimal>(type: "TEXT", nullable: false),
                    TroskoviSmestaja = table.Column<decimal>(type: "TEXT", nullable: false),
                    TroskoviPrevoza = table.Column<decimal>(type: "TEXT", nullable: false),
                    OstaliTroskovi = table.Column<decimal>(type: "TEXT", nullable: false),
                    Akontacija = table.Column<decimal>(type: "TEXT", nullable: false),
                    UkupnoZaIsplatu = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Napomena = table.Column<string>(type: "TEXT", nullable: false),
                    Korisnik = table.Column<string>(type: "TEXT", nullable: false),
                    NalogId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsKnjizeno = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PutniNalozi", x => x.PutniNalogId);
                });

            migrationBuilder.CreateTable(
                name: "KompenzacijeStavke",
                columns: table => new
                {
                    KompenzacijaStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KompenzacijaId = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    StavkaNalogaId = table.Column<int>(type: "INTEGER", nullable: false),
                    BrojDokumenta = table.Column<string>(type: "TEXT", nullable: false),
                    DatumDokumenta = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Strana = table.Column<string>(type: "TEXT", nullable: false),
                    BrojKonta = table.Column<string>(type: "TEXT", nullable: false),
                    IznosFakture = table.Column<decimal>(type: "TEXT", nullable: false),
                    IznosPreostalo = table.Column<decimal>(type: "TEXT", nullable: false),
                    IznosZaKompenzaciju = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KompenzacijeStavke", x => x.KompenzacijaStavkaId);
                    table.ForeignKey(
                        name: "FK_KompenzacijeStavke_Kompenzacije_KompenzacijaId",
                        column: x => x.KompenzacijaId,
                        principalTable: "Kompenzacije",
                        principalColumn: "KompenzacijaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NarudzbeniceStavke",
                columns: table => new
                {
                    NarudzbenicaStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NarudzbenicaId = table.Column<int>(type: "INTEGER", nullable: false),
                    NarudzbenicaDobavljacuNarudzbenicaId = table.Column<int>(type: "INTEGER", nullable: true),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    SifraArtikla = table.Column<string>(type: "TEXT", nullable: false),
                    NazivArtikla = table.Column<string>(type: "TEXT", nullable: false),
                    JedinicaMere = table.Column<string>(type: "TEXT", nullable: false),
                    KolicinaNarucena = table.Column<decimal>(type: "TEXT", nullable: false),
                    KolicinaPristigla = table.Column<decimal>(type: "TEXT", nullable: false),
                    Cena = table.Column<decimal>(type: "TEXT", nullable: false),
                    PdvStopa = table.Column<decimal>(type: "TEXT", nullable: false),
                    IznosNeto = table.Column<decimal>(type: "TEXT", nullable: false),
                    IznosPdv = table.Column<decimal>(type: "TEXT", nullable: false),
                    IznosBruto = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NarudzbeniceStavke", x => x.NarudzbenicaStavkaId);
                    table.ForeignKey(
                        name: "FK_NarudzbeniceStavke_NarudzbeniceDobavljacima_NarudzbenicaDobavljacuNarudzbenicaId",
                        column: x => x.NarudzbenicaDobavljacuNarudzbenicaId,
                        principalTable: "NarudzbeniceDobavljacima",
                        principalColumn: "NarudzbenicaId");
                });

            migrationBuilder.CreateTable(
                name: "PonudeStavke",
                columns: table => new
                {
                    PonudaStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PonudaPredracunId = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    SifraArtikla = table.Column<string>(type: "TEXT", nullable: false),
                    NazivArtikla = table.Column<string>(type: "TEXT", nullable: false),
                    JedinicaMere = table.Column<string>(type: "TEXT", nullable: false),
                    Kolicina = table.Column<decimal>(type: "TEXT", nullable: false),
                    Cena = table.Column<decimal>(type: "TEXT", nullable: false),
                    RabatProcenat = table.Column<decimal>(type: "TEXT", nullable: false),
                    PdvStopa = table.Column<decimal>(type: "TEXT", nullable: false),
                    IznosNeto = table.Column<decimal>(type: "TEXT", nullable: false),
                    IznosPdv = table.Column<decimal>(type: "TEXT", nullable: false),
                    IznosBruto = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PonudeStavke", x => x.PonudaStavkaId);
                    table.ForeignKey(
                        name: "FK_PonudeStavke_PonudePredracuni_PonudaPredracunId",
                        column: x => x.PonudaPredracunId,
                        principalTable: "PonudePredracuni",
                        principalColumn: "PonudaPredracunId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PutniNaloziTroskoviStavke",
                columns: table => new
                {
                    PutniNalogTrosakStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PutniNalogId = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    VrstaTroska = table.Column<string>(type: "TEXT", nullable: false),
                    BrojRacuna = table.Column<string>(type: "TEXT", nullable: false),
                    DatumRacuna = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Iznos = table.Column<decimal>(type: "TEXT", nullable: false),
                    Opis = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PutniNaloziTroskoviStavke", x => x.PutniNalogTrosakStavkaId);
                    table.ForeignKey(
                        name: "FK_PutniNaloziTroskoviStavke_PutniNalozi_PutniNalogId",
                        column: x => x.PutniNalogId,
                        principalTable: "PutniNalozi",
                        principalColumn: "PutniNalogId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StavkeNaloga_MestoTroskaId",
                table: "StavkeNaloga",
                column: "MestoTroskaId");

            migrationBuilder.CreateIndex(
                name: "IX_KompenzacijeStavke_KompenzacijaId",
                table: "KompenzacijeStavke",
                column: "KompenzacijaId");

            migrationBuilder.CreateIndex(
                name: "IX_NarudzbeniceStavke_NarudzbenicaDobavljacuNarudzbenicaId",
                table: "NarudzbeniceStavke",
                column: "NarudzbenicaDobavljacuNarudzbenicaId");

            migrationBuilder.CreateIndex(
                name: "IX_PonudeStavke_PonudaPredracunId",
                table: "PonudeStavke",
                column: "PonudaPredracunId");

            migrationBuilder.CreateIndex(
                name: "IX_PutniNaloziTroskoviStavke_PutniNalogId",
                table: "PutniNaloziTroskoviStavke",
                column: "PutniNalogId");

            migrationBuilder.AddForeignKey(
                name: "FK_StavkeNaloga_MestaTroska_MestoTroskaId",
                table: "StavkeNaloga",
                column: "MestoTroskaId",
                principalTable: "MestaTroska",
                principalColumn: "MestoTroskaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StavkeNaloga_MestaTroska_MestoTroskaId",
                table: "StavkeNaloga");

            migrationBuilder.DropTable(
                name: "BlagajnickiNalozi");

            migrationBuilder.DropTable(
                name: "KompenzacijeStavke");

            migrationBuilder.DropTable(
                name: "MestaTroska");

            migrationBuilder.DropTable(
                name: "NarudzbeniceStavke");

            migrationBuilder.DropTable(
                name: "PonudeStavke");

            migrationBuilder.DropTable(
                name: "PutniNaloziTroskoviStavke");

            migrationBuilder.DropTable(
                name: "Kompenzacije");

            migrationBuilder.DropTable(
                name: "NarudzbeniceDobavljacima");

            migrationBuilder.DropTable(
                name: "PonudePredracuni");

            migrationBuilder.DropTable(
                name: "PutniNalozi");

            migrationBuilder.DropIndex(
                name: "IX_StavkeNaloga_MestoTroskaId",
                table: "StavkeNaloga");

            migrationBuilder.DropColumn(
                name: "MestoTroskaId",
                table: "StavkeNaloga");
        }
    }
}
