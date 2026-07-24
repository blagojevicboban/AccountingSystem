using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingData.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Artikli",
                columns: table => new
                {
                    ArtikalId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SifraArtikla = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    JedinicaMere = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Pakovanje = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    TarifniBroj = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Barkod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    NabavnaCena = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    ProdajnaCena = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    Vrsta = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artikli", x => x.ArtikalId);
                });

            migrationBuilder.CreateTable(
                name: "Firme",
                columns: table => new
                {
                    FirmaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sifra = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Adresa = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PttIMesto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Telefon = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ZiroRacun = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Pib = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    MaticniBroj = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DatumKreiranja = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Firme", x => x.FirmaId);
                });

            migrationBuilder.CreateTable(
                name: "Kalkulacije",
                columns: table => new
                {
                    KalkulacijaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojKalkulacije = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SifraDobavljaca = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    BrojOtpremnice = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    DatumOtpremnice = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BrojRacuna = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    DatumRacuna = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NabavnaVrednost = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    TransportniTroskovi = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    TroskoviUskladistenja = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UtovarIstovar = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    TransportnoOsiguranje = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    OstaliTroskovi = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    SvegaTroskovi = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    SvegaNabavno = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Razlika = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Porez = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    ProdajnaVrednost = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    SifraMagacina = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    IsKnjizen = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kalkulacije", x => x.KalkulacijaId);
                });

            migrationBuilder.CreateTable(
                name: "KarticeKonta",
                columns: table => new
                {
                    KarticaKontaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    BrojKonta = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DatumNaloga = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BrojNaloga = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    OpisPromeneKod = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    BrojDokumenta = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    TekuceDuguje = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    TekucePotrazuje = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UkupnoDuguje = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UkupnoPotrazuje = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Saldo = table.Column<decimal>(type: "decimal(18, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KarticeKonta", x => x.KarticaKontaId);
                });

            migrationBuilder.CreateTable(
                name: "Konta",
                columns: table => new
                {
                    KontoId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojKonta = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NazivKonta = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    VrstaKonta = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsSintetika = table.Column<bool>(type: "INTEGER", nullable: false),
                    Klasa = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Konta", x => x.KontoId);
                });

            migrationBuilder.CreateTable(
                name: "Korisnici",
                columns: table => new
                {
                    KorisnikId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KorisnickoIme = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LozinkaHash = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ImeIPrezime = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Uloga = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    PoslednjaPrijava = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Korisnici", x => x.KorisnikId);
                });

            migrationBuilder.CreateTable(
                name: "Magacini",
                columns: table => new
                {
                    MagacinId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SifraMagacina = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NazivMagacina = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OdgovornoLice = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    VrstaMagacina = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Magacini", x => x.MagacinId);
                });

            migrationBuilder.CreateTable(
                name: "MaloprodajneKalkulacije",
                columns: table => new
                {
                    MaloprodajnaKalkulacijaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SifraProdavnice = table.Column<int>(type: "INTEGER", nullable: false),
                    BrojKalkulacije = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SifraMagacinaPrima = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    SifraMagacinaDaje = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    SifraDobavljaca = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    BrojOtpremnice = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    DatumOtpremnice = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BrojRacuna = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    DatumRacuna = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TransportniTroskovi = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    TroskoviUskladistenja = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UtovarIstovar = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    TransportnoOsiguranje = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    OstaliTroskovi = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    IsKnjizen = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsTrgovinskiKnjizen = table.Column<bool>(type: "INTEGER", nullable: false),
                    SvegaTroskovi = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    RabatPri = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    NabavnaVrednost = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    SvegaNabavno = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Razlika = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Porez = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    ProdajnaVrednost = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    RabatIznos = table.Column<decimal>(type: "decimal(18, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaloprodajneKalkulacije", x => x.MaloprodajnaKalkulacijaId);
                });

            migrationBuilder.CreateTable(
                name: "MaterijalneKartice",
                columns: table => new
                {
                    MaterijalnaKarticaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SifraMagacina = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SifraArtikla = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    DatumPromene = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OpisPromene = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Ulaz = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Izlaz = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Stanje = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Cena = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    CenaIzlaz = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    Duguje = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Potrazuje = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Saldo = table.Column<decimal>(type: "decimal(18, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterijalneKartice", x => x.MaterijalnaKarticaId);
                });

            migrationBuilder.CreateTable(
                name: "Nalozi",
                columns: table => new
                {
                    NalogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojNaloga = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DatumNaloga = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VrstaNaloga = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Opis = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    IsKnjizen = table.Column<bool>(type: "INTEGER", nullable: false),
                    DatumKnjiženja = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UkupnoDuguje = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    UkupnoPotrazuje = table.Column<decimal>(type: "decimal(18, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nalozi", x => x.NalogId);
                });

            migrationBuilder.CreateTable(
                name: "Partneri",
                columns: table => new
                {
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SifraPartnera = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Adresa = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PttIMesto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Pib = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    MaticniBroj = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Telefon = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ZiroRacun = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    KontoPartnera = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partneri", x => x.PartnerId);
                });

            migrationBuilder.CreateTable(
                name: "PrimopredajaNalozi",
                columns: table => new
                {
                    PrimopredajaNalogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojNaloga = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SifraMagacinaDaje = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SifraMagacinaPrima = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsKnjizen = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrimopredajaNalozi", x => x.PrimopredajaNalogId);
                });

            migrationBuilder.CreateTable(
                name: "TrebovanjeNalozi",
                columns: table => new
                {
                    TrebovanjeNalogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojNaloga = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SifraMagacina = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsKnjizen = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrebovanjeNalozi", x => x.TrebovanjeNalogId);
                });

            migrationBuilder.CreateTable(
                name: "UlazNalozi",
                columns: table => new
                {
                    UlazNalogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrojNaloga = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Datum = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SifraMagacina = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    BrojRacuna = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    DatumRacuna = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsKnjizen = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UlazNalozi", x => x.UlazNalogId);
                });

            migrationBuilder.CreateTable(
                name: "StavkeNaloga",
                columns: table => new
                {
                    StavkaNalogaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NalogId = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    BrojKonta = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    BrojDokumenta = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DatumDokumenta = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ValutaDospela = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Opis = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    Duguje = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Potrazuje = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    PartnerId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StavkeNaloga", x => x.StavkaNalogaId);
                    table.ForeignKey(
                        name: "FK_StavkeNaloga_Nalozi_NalogId",
                        column: x => x.NalogId,
                        principalTable: "Nalozi",
                        principalColumn: "NalogId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StavkeNaloga_Partneri_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partneri",
                        principalColumn: "PartnerId");
                });

            migrationBuilder.CreateTable(
                name: "PrimopredajaStavke",
                columns: table => new
                {
                    PrimopredajaStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PrimopredajaNalogId = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    SifraArtikla = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Kolicina = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Cena = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    Iznos = table.Column<decimal>(type: "decimal(18, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrimopredajaStavke", x => x.PrimopredajaStavkaId);
                    table.ForeignKey(
                        name: "FK_PrimopredajaStavke_PrimopredajaNalozi_PrimopredajaNalogId",
                        column: x => x.PrimopredajaNalogId,
                        principalTable: "PrimopredajaNalozi",
                        principalColumn: "PrimopredajaNalogId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrebovanjeStavke",
                columns: table => new
                {
                    TrebovanjeStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrebovanjeNalogId = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    SifraArtikla = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Kolicina = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Cena = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    Iznos = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    KontoTroska = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrebovanjeStavke", x => x.TrebovanjeStavkaId);
                    table.ForeignKey(
                        name: "FK_TrebovanjeStavke_TrebovanjeNalozi_TrebovanjeNalogId",
                        column: x => x.TrebovanjeNalogId,
                        principalTable: "TrebovanjeNalozi",
                        principalColumn: "TrebovanjeNalogId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UlazStavke",
                columns: table => new
                {
                    UlazStavkaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UlazNalogId = table.Column<int>(type: "INTEGER", nullable: false),
                    RedniBroj = table.Column<int>(type: "INTEGER", nullable: false),
                    SifraArtikla = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Kolicina = table.Column<decimal>(type: "decimal(18, 2)", nullable: false),
                    Cena = table.Column<decimal>(type: "decimal(18, 4)", nullable: false),
                    Iznos = table.Column<decimal>(type: "decimal(18, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UlazStavke", x => x.UlazStavkaId);
                    table.ForeignKey(
                        name: "FK_UlazStavke_UlazNalozi_UlazNalogId",
                        column: x => x.UlazNalogId,
                        principalTable: "UlazNalozi",
                        principalColumn: "UlazNalogId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Korisnici",
                columns: new[] { "KorisnikId", "ImeIPrezime", "IsActive", "KorisnickoIme", "LozinkaHash", "PoslednjaPrijava", "Uloga" },
                values: new object[] { 1, "Administrator", true, "admin", "PBKDF2$100000$CnYWiALqycqWTueq6ayEvQ==$hvm9e8z3e+KVeRsego3azOuoTp3q64deikPgUB9/D4o=", null, "Administrator" });

            migrationBuilder.CreateIndex(
                name: "IX_Artikli_SifraArtikla",
                table: "Artikli",
                column: "SifraArtikla");

            migrationBuilder.CreateIndex(
                name: "IX_Firme_Sifra",
                table: "Firme",
                column: "Sifra",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kalkulacije_BrojKalkulacije",
                table: "Kalkulacije",
                column: "BrojKalkulacije");

            migrationBuilder.CreateIndex(
                name: "IX_KarticeKonta_BrojKonta",
                table: "KarticeKonta",
                column: "BrojKonta");

            migrationBuilder.CreateIndex(
                name: "IX_Konta_BrojKonta",
                table: "Konta",
                column: "BrojKonta",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Korisnici_KorisnickoIme",
                table: "Korisnici",
                column: "KorisnickoIme",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaloprodajneKalkulacije_BrojKalkulacije",
                table: "MaloprodajneKalkulacije",
                column: "BrojKalkulacije");

            migrationBuilder.CreateIndex(
                name: "IX_MaterijalneKartice_SifraMagacina_SifraArtikla",
                table: "MaterijalneKartice",
                columns: new[] { "SifraMagacina", "SifraArtikla" });

            migrationBuilder.CreateIndex(
                name: "IX_Nalozi_BrojNaloga",
                table: "Nalozi",
                column: "BrojNaloga");

            migrationBuilder.CreateIndex(
                name: "IX_Partneri_SifraPartnera",
                table: "Partneri",
                column: "SifraPartnera");

            migrationBuilder.CreateIndex(
                name: "IX_PrimopredajaNalozi_BrojNaloga",
                table: "PrimopredajaNalozi",
                column: "BrojNaloga");

            migrationBuilder.CreateIndex(
                name: "IX_PrimopredajaStavke_PrimopredajaNalogId",
                table: "PrimopredajaStavke",
                column: "PrimopredajaNalogId");

            migrationBuilder.CreateIndex(
                name: "IX_StavkeNaloga_NalogId",
                table: "StavkeNaloga",
                column: "NalogId");

            migrationBuilder.CreateIndex(
                name: "IX_StavkeNaloga_PartnerId",
                table: "StavkeNaloga",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_TrebovanjeNalozi_BrojNaloga",
                table: "TrebovanjeNalozi",
                column: "BrojNaloga");

            migrationBuilder.CreateIndex(
                name: "IX_TrebovanjeStavke_TrebovanjeNalogId",
                table: "TrebovanjeStavke",
                column: "TrebovanjeNalogId");

            migrationBuilder.CreateIndex(
                name: "IX_UlazNalozi_BrojNaloga",
                table: "UlazNalozi",
                column: "BrojNaloga");

            migrationBuilder.CreateIndex(
                name: "IX_UlazStavke_UlazNalogId",
                table: "UlazStavke",
                column: "UlazNalogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Artikli");

            migrationBuilder.DropTable(
                name: "Firme");

            migrationBuilder.DropTable(
                name: "Kalkulacije");

            migrationBuilder.DropTable(
                name: "KarticeKonta");

            migrationBuilder.DropTable(
                name: "Konta");

            migrationBuilder.DropTable(
                name: "Korisnici");

            migrationBuilder.DropTable(
                name: "Magacini");

            migrationBuilder.DropTable(
                name: "MaloprodajneKalkulacije");

            migrationBuilder.DropTable(
                name: "MaterijalneKartice");

            migrationBuilder.DropTable(
                name: "PrimopredajaStavke");

            migrationBuilder.DropTable(
                name: "StavkeNaloga");

            migrationBuilder.DropTable(
                name: "TrebovanjeStavke");

            migrationBuilder.DropTable(
                name: "UlazStavke");

            migrationBuilder.DropTable(
                name: "PrimopredajaNalozi");

            migrationBuilder.DropTable(
                name: "Nalozi");

            migrationBuilder.DropTable(
                name: "Partneri");

            migrationBuilder.DropTable(
                name: "TrebovanjeNalozi");

            migrationBuilder.DropTable(
                name: "UlazNalozi");
        }
    }
}
