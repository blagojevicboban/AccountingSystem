using AccountingData.Models;
using AccountingData.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AccountingApp.Services;

public class PdfReportService
{
    public static byte[] GenerisiDnevnikPdf(Firma firma, List<Nalog> nalozi, Dictionary<int, string>? promene = null)
    {
        promene ??= new Dictionary<int, string>();
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Text(firma.Naziv).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                    col.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"} | Žiro: {firma.ZiroRacun ?? "---"}").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(10).Text("DNEVNIK KNJIŽENJA (GLAVNA KNJIGA)").Bold().FontSize(16).AlignCenter();
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(45);
                            columns.ConstantColumn(60);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(65);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(80);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Nalog").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Datum").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Dokument / Opis").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Konto").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Promena").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Duguje (RSD)").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Potražuje (RSD)").Bold().AlignRight();
                        });

                        decimal zbirDuguje = 0;
                        decimal zbirPotrazuje = 0;

                        foreach (var nalog in nalozi)
                        {
                            foreach (var st in nalog.Stavke)
                            {
                                zbirDuguje += st.Duguje;
                                zbirPotrazuje += st.Potrazuje;

                                string opisPromene = st.PromenaKod.HasValue && promene.TryGetValue(st.PromenaKod.Value, out var op) ? op : "";
                                string prikazDokumentOpis = !string.IsNullOrWhiteSpace(st.BrojDokumenta)
                                    ? st.BrojDokumenta
                                    : (!string.IsNullOrWhiteSpace(st.Opis) && !st.Opis.Equals(opisPromene, StringComparison.OrdinalIgnoreCase) ? st.Opis : (nalog.Opis ?? ""));

                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(nalog.BrojNaloga);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(nalog.DatumNaloga.ToString("dd.MM.yyyy"));
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(prikazDokumentOpis);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(st.BrojKonta);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(opisPromene);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text($"{st.Duguje:N2}").AlignRight();
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text($"{st.Potrazuje:N2}").AlignRight();
                            }
                        }

                        table.Cell().ColumnSpan(5).PaddingVertical(3).PaddingHorizontal(4).Text("UKUPAN PROMET DNEVNIKA:").Bold().AlignRight();
                        table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text($"{zbirDuguje:N2}").Bold().AlignRight();
                        table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text($"{zbirPotrazuje:N2}").Bold().AlignRight();
                    });
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiKarticuPdf(Firma firma, Konto konto, List<KarticaRed> stavke,
        DateTime? odDatuma = null, DateTime? doDatuma = null)
    {
        return Document.Create(container =>
        {
            container.Page(page => ComposeKarticaPage(page, firma, konto, stavke, odDatuma, doDatuma));
        }).GeneratePdf();
    }

    public static byte[] GenerisiViseKarticaPdf(Firma firma, List<(Konto Konto, List<KarticaRed> Stavke)> kartice,
        DateTime? odDatuma = null, DateTime? doDatuma = null)
    {
        return Document.Create(container =>
        {
            foreach (var (konto, stavke) in kartice)
            {
                container.Page(page => ComposeKarticaPage(page, firma, konto, stavke, odDatuma, doDatuma));
            }
        }).GeneratePdf();
    }

    private static void ComposeKarticaPage(PageDescriptor page, Firma firma, Konto konto, List<KarticaRed> stavke,
        DateTime? odDatuma, DateTime? doDatuma)
    {
        page.Size(PageSizes.A4);
        page.Margin(1.5f, Unit.Centimetre);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

        page.Header().Column(col =>
        {
            col.Item().Text(firma.Naziv).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
            col.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"} | Žiro: {firma.ZiroRacun ?? "---"}").FontSize(9).FontColor(Colors.Grey.Medium);
            col.Item().PaddingTop(10).Text("KARTICA KONTA").Bold().FontSize(16).AlignCenter();
            col.Item().Text($"{konto.BrojKonta} — {konto.NazivKonta}").FontSize(12).AlignCenter();
            if (odDatuma.HasValue || doDatuma.HasValue)
                col.Item().Text($"Period: {odDatuma?.ToString("dd.MM.yyyy") ?? "---"} - {doDatuma?.ToString("dd.MM.yyyy") ?? "---"}").FontSize(9).AlignCenter().FontColor(Colors.Grey.Medium);
            col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });

        page.Content().PaddingVertical(10).Column(col =>
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(60);
                    columns.ConstantColumn(45);
                    columns.RelativeColumn(3);
                    columns.ConstantColumn(65);
                    columns.ConstantColumn(70);
                    columns.ConstantColumn(70);
                    columns.ConstantColumn(70);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Datum").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Nalog").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Opis").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Promena").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Duguje").Bold().AlignRight();
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Potražuje").Bold().AlignRight();
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Saldo").Bold().AlignRight();
                });

                decimal zbirDuguje = 0, zbirPotrazuje = 0;

                foreach (var s in stavke)
                {
                    zbirDuguje += s.Duguje;
                    zbirPotrazuje += s.Potrazuje;

                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(s.Datum.ToString("dd.MM.yyyy"));
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(s.BrojNaloga);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(s.Opis ?? "");
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(s.OpisPromene ?? "");
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text($"{s.Duguje:N2}").AlignRight();
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text($"{s.Potrazuje:N2}").AlignRight();
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text($"{s.Saldo:N2}").AlignRight();
                }

                table.Cell().ColumnSpan(4).PaddingVertical(3).PaddingHorizontal(4).Text("UKUPNO:").Bold().AlignRight();
                table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text($"{zbirDuguje:N2}").Bold().AlignRight();
                table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text($"{zbirPotrazuje:N2}").Bold().AlignRight();
                table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text($"{(stavke.Count > 0 ? stavke[^1].Saldo : 0m):N2}").Bold().AlignRight();
            });
        });

        page.Footer().AlignRight().Text(x =>
        {
            x.Span("Stranica ");
            x.CurrentPageNumber();
            x.Span(" od ");
            x.TotalPages();
        });
    }

    public static byte[] GenerisiIOSPdf(Firma firma, Partner partner, List<KarticaRed> stavke)
    {
        var grupa = new IosPartnerGrupa
        {
            SifraPartnera = partner.SifraPartnera,
            NazivPartnera = partner.Naziv,
            Konto = partner.KontoPartnera ?? partner.SifraPartnera,
            Adresa = partner.Adresa,
            PttIMesto = partner.PttIMesto,
            Pib = partner.Pib,
            Partner = partner,
            Stavke = stavke
        };
        return GenerisiZbirniIOSPdf(firma, new List<IosPartnerGrupa> { grupa });
    }

    public static byte[] GenerisiZbirniIOSPdf(Firma firma, List<IosPartnerGrupa> grupe, string? odKonta = null, string? doKonta = null, DateTime? odDatuma = null, DateTime? doDatuma = null)
    {
        return Document.Create(container =>
        {
            if (grupe.Count == 0)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(firma.Naziv).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                        col.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"} | Žiro: {firma.ZiroRacun ?? "---"}").FontSize(9).FontColor(Colors.Grey.Medium);
                        col.Item().PaddingTop(10).Text("IZVOD OTVORENIH STAVKI (IOS)").Bold().FontSize(16).AlignCenter();
                        col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingVertical(30).AlignCenter().Text("Nema otvorenih stavki za izabrani opseg i kriterijume.").FontSize(12).FontColor(Colors.Grey.Medium).Italic();
                });
                return;
            }

            foreach (var grupa in grupe)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.2f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Calibri"));

                    // Zaglavlje po uzoru na 4-otvorene stavke.txt
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Item().Text(firma.Naziv).Bold().FontSize(12).FontColor(Colors.Blue.Medium);
                                left.Item().Text(firma.Adresa ?? "").FontSize(9);
                                left.Item().Text(firma.PttIMesto ?? "").FontSize(9);
                                if (!string.IsNullOrWhiteSpace(firma.Telefon))
                                    left.Item().Text($"Tel: {firma.Telefon}").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                                if (!string.IsNullOrWhiteSpace(firma.ZiroRacun))
                                    left.Item().Text($"Žiro račun: {firma.ZiroRacun}").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                                if (!string.IsNullOrWhiteSpace(firma.Pib))
                                    left.Item().Text($"PIB: {firma.Pib}").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                            });

                            row.RelativeItem().AlignRight().Column(right =>
                            {
                                right.Item().Text($"DATUM: {DateTime.Now:dd.MM.yyyy}").FontSize(9).Bold();
                                right.Item().PaddingTop(4).Text($"DUŽNIK: {grupa.Konto} / {grupa.SifraPartnera}").FontSize(10).Bold();
                                right.Item().Text(grupa.NazivPartnera).FontSize(11).Bold();
                                if (!string.IsNullOrWhiteSpace(grupa.Adresa))
                                    right.Item().Text($"{grupa.Adresa}, {grupa.PttIMesto}").FontSize(9);
                                if (!string.IsNullOrWhiteSpace(grupa.Pib))
                                    right.Item().Text($"PIB: {grupa.Pib}").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                            });
                        });

                        col.Item().PaddingTop(10).Text("I Z V O D   O T V O R E N I H   S T A V K I").Bold().FontSize(14).AlignCenter();
                        col.Item().PaddingTop(2).AlignCenter().Text("___________________________________________").FontSize(10).FontColor(Colors.Grey.Medium);

                        decimal netoSaldo = grupa.Saldo;
                        string uKorist = netoSaldo >= 0 ? "našu korist" : "Vašu korist";

                        col.Item().PaddingTop(8).Text($"Na osnovu naše evidencije utvrdili smo saldo od {Math.Abs(netoSaldo):N2} din. u {uKorist}.").FontSize(9.5f);
                        col.Item().Text("Molimo Vas da uporedite stanje na kartici sa našim stanjem.").FontSize(9.5f);
                        col.Item().PaddingTop(6).LineHorizontal(0.8f).LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingVertical(6).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(58);
                                columns.ConstantColumn(42);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.ConstantColumn(75);
                                columns.ConstantColumn(75);
                                columns.ConstantColumn(80);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Datum").Bold().FontSize(8);
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Nalog").Bold().FontSize(8);
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Opis promene").Bold().FontSize(8);
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Broj dokumenta").Bold().FontSize(8);
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Duguje").Bold().FontSize(8).AlignRight();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Potražuje").Bold().FontSize(8).AlignRight();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Saldo").Bold().FontSize(8).AlignRight();
                            });

                            decimal zbirDuguje = 0, zbirPotrazuje = 0;

                            foreach (var s in grupa.Stavke)
                            {
                                zbirDuguje += s.Duguje;
                                zbirPotrazuje += s.Potrazuje;

                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(s.Datum.ToString("dd.MM.yyyy")).FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(s.BrojNaloga.ToString()).FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(s.Opis ?? "").FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(s.OpisPromene ?? "").FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{s.Duguje:N2}").FontSize(8).AlignRight();
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{s.Potrazuje:N2}").FontSize(8).AlignRight();
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{s.Saldo:N2}").FontSize(8).AlignRight();
                            }

                            table.Cell().ColumnSpan(4).PaddingVertical(3).PaddingHorizontal(2).Text("UKUPNO:").Bold().FontSize(8.5f).AlignRight();
                            table.Cell().PaddingVertical(3).PaddingHorizontal(2).Text($"{zbirDuguje:N2}").Bold().FontSize(8.5f).AlignRight();
                            table.Cell().PaddingVertical(3).PaddingHorizontal(2).Text($"{zbirPotrazuje:N2}").Bold().FontSize(8.5f).AlignRight();
                            table.Cell().PaddingVertical(3).PaddingHorizontal(2).Text($"{grupa.Saldo:N2}").Bold().FontSize(8.5f).AlignRight();
                        });

                        if (grupa.Stavke.Count == 0)
                        {
                            col.Item().PaddingTop(15).AlignCenter().Text("Nema proknjiženih otvorenih stavki.").FontColor(Colors.Grey.Medium).Italic();
                        }

                        // Donji blok po uzoru na 4-otvorene stavke.txt (potvrda i osporavanje)
                        col.Item().PaddingTop(15).Column(potpisCol =>
                        {
                            potpisCol.Item().Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Pošiljalac izvoda").Bold().FontSize(8.5f);
                                    c.Item().PaddingTop(12).Text("M.P.").FontSize(8);
                                    c.Item().PaddingTop(12).Text("Mesto i datum: ______________________").FontSize(8);
                                });

                                row.RelativeItem().AlignRight().Column(c =>
                                {
                                    c.Item().Text("Potvrđujem saglasnost otvorenih stavki").Bold().FontSize(8.5f);
                                    c.Item().PaddingTop(12).Text("M.P.").FontSize(8);
                                    c.Item().PaddingTop(12).Text("Mesto i datum: ______________________").FontSize(8);
                                });
                            });

                            potpisCol.Item().PaddingTop(15).Text("NAPOMENA: Osporavamo iskazano stanje u CELINI - DELIMIČNO za iznos").Bold().FontSize(8.5f);
                            potpisCol.Item().PaddingTop(4).Text("od ____________________ din. iz sledećih razloga:").FontSize(8.5f);
                            potpisCol.Item().PaddingTop(6).Text("_________________________________________________________________________________________________________").FontColor(Colors.Grey.Medium).FontSize(8);
                            potpisCol.Item().PaddingTop(6).Text("_________________________________________________________________________________________________________").FontColor(Colors.Grey.Medium).FontSize(8);

                            potpisCol.Item().PaddingTop(12).AlignRight().Column(c =>
                            {
                                c.Item().Text("Dužnik,").FontSize(8.5f);
                                c.Item().PaddingTop(12).Text("_______________________ M.P.").FontSize(8.5f);
                            });
                        });
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("Stranica ");
                        x.CurrentPageNumber();
                        x.Span(" od ");
                        x.TotalPages();
                    });
                });
            }
        }).GeneratePdf();
    }

    public static byte[] GenerisiKamataPdf(Firma firma, Partner partner, List<KamataStavka> stavke, DateTime datumObracuna)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Text(firma.Naziv).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                    col.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"} | Žiro: {firma.ZiroRacun ?? "---"}").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(10).Text("OBRAČUN ZATEZNE KAMATE").Bold().FontSize(16).AlignCenter();
                    col.Item().Text($"{partner.SifraPartnera} — {partner.Naziv}").FontSize(12).AlignCenter();
                    col.Item().Text($"Datum obračuna: {datumObracuna:dd.MM.yyyy}").FontSize(9).FontColor(Colors.Grey.Medium).AlignCenter();
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(70);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(80);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Datum duga").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Nalog").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Opis").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Iznos").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Dana").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Kamata").Bold().AlignRight();
                        });

                        decimal zbirIznos = 0, zbirKamata = 0;

                        foreach (var s in stavke)
                        {
                            zbirIznos += s.Iznos;
                            zbirKamata += s.ObracunataKamata;

                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(s.Datum.ToString("dd.MM.yyyy"));
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(s.BrojNaloga.ToString());
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(s.Opis ?? "");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{s.Iznos:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{s.BrojDanaKasnjenja}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{s.ObracunataKamata:N2}").AlignRight();
                        }

                        table.Cell().ColumnSpan(3).Padding(6).Text("UKUPNO:").Bold().AlignRight();
                        table.Cell().Padding(6).Text($"{zbirIznos:N2}").Bold().AlignRight();
                        table.Cell().Padding(6).Text("");
                        table.Cell().Padding(6).Text($"{zbirKamata:N2}").Bold().AlignRight();
                    });

                    if (stavke.Count == 0)
                    {
                        col.Item().PaddingTop(20).AlignCenter().Text("Nema dugovnih stavki sa kašnjenjem na dati datum obračuna.").FontColor(Colors.Grey.Medium).Italic();
                    }
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiBrutoBilansPdf(Firma firma, List<BrutoBilansRed> redovi,
        string naslov = "BRUTO BILANS", DateTime? odDatuma = null, DateTime? doDatuma = null)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.0f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Text(firma.Naziv).Bold().FontSize(12).FontColor(Colors.Blue.Medium);
                    col.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"} | Žiro: {firma.ZiroRacun ?? "---"}").FontSize(8).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(6).Text(naslov).Bold().FontSize(14).AlignCenter();
                    if (odDatuma.HasValue || doDatuma.HasValue)
                        col.Item().Text($"Period: {odDatuma?.ToString("dd.MM.yyyy") ?? "---"} - {doDatuma?.ToString("dd.MM.yyyy") ?? "---"}").FontSize(8).AlignCenter().FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(6).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(50);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(82);
                            columns.ConstantColumn(82);
                            columns.ConstantColumn(82);
                            columns.ConstantColumn(82);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(3).Text("Konto").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(3).Text("Naziv konta").Bold().FontSize(8);
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(3).Text("Duguje").Bold().FontSize(8).AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(3).Text("Potražuje").Bold().FontSize(8).AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(3).Text("Saldo duguje").Bold().FontSize(8).AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(3).Text("Saldo potražuje").Bold().FontSize(8).AlignRight();
                        });

                        decimal zbirDuguje = 0, zbirPotrazuje = 0, zbirSaldoDuguje = 0, zbirSaldoPotrazuje = 0;

                        foreach (var r in redovi)
                        {
                            if (r.Tip != BrutoBilansRedTip.Detalj)
                            {
                                var pozadina = r.Tip == BrutoBilansRedTip.KlasaTotal ? Colors.Grey.Lighten2 : Colors.Grey.Lighten4;
                                table.Cell().ColumnSpan(2).Background(pozadina).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(3).Text(r.NazivKonta).Bold().FontSize(8);
                                table.Cell().Background(pozadina).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(3).Text($"{r.Duguje:N2}").Bold().FontSize(8).AlignRight();
                                table.Cell().Background(pozadina).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(3).Text($"{r.Potrazuje:N2}").Bold().FontSize(8).AlignRight();
                                table.Cell().Background(pozadina).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(3).Text($"{r.SaldoDuguje:N2}").Bold().FontSize(8).AlignRight();
                                table.Cell().Background(pozadina).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(3).Text($"{r.SaldoPotrazuje:N2}").Bold().FontSize(8).AlignRight();
                                continue;
                            }

                            zbirDuguje += r.Duguje;
                            zbirPotrazuje += r.Potrazuje;
                            zbirSaldoDuguje += r.SaldoDuguje;
                            zbirSaldoPotrazuje += r.SaldoPotrazuje;

                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(3).Text(r.BrojKonta).FontSize(8);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(3).Text(r.NazivKonta).FontSize(8);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(3).Text($"{r.Duguje:N2}").FontSize(8).AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(3).Text($"{r.Potrazuje:N2}").FontSize(8).AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(3).Text($"{r.SaldoDuguje:N2}").FontSize(8).AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(3).Text($"{r.SaldoPotrazuje:N2}").FontSize(8).AlignRight();
                        }

                        table.Cell().ColumnSpan(2).PaddingVertical(4).PaddingHorizontal(3).Text("UKUPNO:").Bold().FontSize(8.5f).AlignRight();
                        table.Cell().PaddingVertical(4).PaddingHorizontal(3).Text($"{zbirDuguje:N2}").Bold().FontSize(8.5f).AlignRight();
                        table.Cell().PaddingVertical(4).PaddingHorizontal(3).Text($"{zbirPotrazuje:N2}").Bold().FontSize(8.5f).AlignRight();
                        table.Cell().PaddingVertical(4).PaddingHorizontal(3).Text($"{zbirSaldoDuguje:N2}").Bold().FontSize(8.5f).AlignRight();
                        table.Cell().PaddingVertical(4).PaddingHorizontal(3).Text($"{zbirSaldoPotrazuje:N2}").Bold().FontSize(8.5f).AlignRight();
                    });
                });


                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiZakljucniListPdf(Firma firma, List<ZakljucniListRed> redovi,
        string naslov = "ZAKLJUČNI LIST", DateTime? odDatuma = null, DateTime? doDatuma = null)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.0f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(7.5f).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Text(firma.Naziv).Bold().FontSize(12).FontColor(Colors.Blue.Medium);
                    col.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"} | Žiro: {firma.ZiroRacun ?? "---"}").FontSize(8).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(4).Text(naslov).Bold().FontSize(14).AlignCenter();
                    if (odDatuma.HasValue || doDatuma.HasValue)
                        col.Item().Text($"Period: {odDatuma?.ToString("dd.MM.yyyy") ?? "---"} - {doDatuma?.ToString("dd.MM.yyyy") ?? "---"}").FontSize(8).AlignCenter().FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(6).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(45);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(75);
                            columns.ConstantColumn(75);
                            columns.ConstantColumn(75);
                            columns.ConstantColumn(75);
                            columns.ConstantColumn(75);
                            columns.ConstantColumn(75);
                            columns.ConstantColumn(75);
                            columns.ConstantColumn(75);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Konto").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Naziv konta").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Poč. Duguje").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Poč. Potražuje").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Prom. Duguje").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Prom. Potražuje").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Ukupno Duguje").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Ukupno Potražuje").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Saldo Duguje").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Saldo Potražuje").Bold().AlignRight();
                        });

                        decimal zbirPocDug = 0, zbirPocPot = 0;
                        decimal zbirPromDug = 0, zbirPromPot = 0;
                        decimal zbirUkDug = 0, zbirUkPot = 0;
                        decimal zbirSalDug = 0, zbirSalPot = 0;

                        foreach (var r in redovi)
                        {
                            if (r.Tip != BrutoBilansRedTip.Detalj)
                            {
                                var pozadina = Colors.Grey.Lighten2;
                                table.Cell().ColumnSpan(2).Background(pozadina).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(r.NazivKonta).Bold();
                                table.Cell().Background(pozadina).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{r.PocetnoDuguje:N2}").Bold().AlignRight();
                                table.Cell().Background(pozadina).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{r.PocetnoPotrazuje:N2}").Bold().AlignRight();
                                table.Cell().Background(pozadina).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{r.PrometDuguje:N2}").Bold().AlignRight();
                                table.Cell().Background(pozadina).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{r.PrometPotrazuje:N2}").Bold().AlignRight();
                                table.Cell().Background(pozadina).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{r.UkupnoDuguje:N2}").Bold().AlignRight();
                                table.Cell().Background(pozadina).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{r.UkupnoPotrazuje:N2}").Bold().AlignRight();
                                table.Cell().Background(pozadina).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{r.SaldoDuguje:N2}").Bold().AlignRight();
                                table.Cell().Background(pozadina).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{r.SaldoPotrazuje:N2}").Bold().AlignRight();
                                continue;
                            }

                            zbirPocDug += r.PocetnoDuguje;
                            zbirPocPot += r.PocetnoPotrazuje;
                            zbirPromDug += r.PrometDuguje;
                            zbirPromPot += r.PrometPotrazuje;
                            zbirUkDug += r.UkupnoDuguje;
                            zbirUkPot += r.UkupnoPotrazuje;
                            zbirSalDug += r.SaldoDuguje;
                            zbirSalPot += r.SaldoPotrazuje;

                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(r.BrojKonta);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(r.NazivKonta);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{r.PocetnoDuguje:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{r.PocetnoPotrazuje:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{r.PrometDuguje:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{r.PrometPotrazuje:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{r.UkupnoDuguje:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{r.UkupnoPotrazuje:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{r.SaldoDuguje:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{r.SaldoPotrazuje:N2}").AlignRight();
                        }

                        table.Cell().ColumnSpan(2).PaddingVertical(4).PaddingHorizontal(2).Text("UKUPNO:").Bold().FontSize(8).AlignRight();
                        table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text($"{zbirPocDug:N2}").Bold().FontSize(8).AlignRight();
                        table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text($"{zbirPocPot:N2}").Bold().FontSize(8).AlignRight();
                        table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text($"{zbirPromDug:N2}").Bold().FontSize(8).AlignRight();
                        table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text($"{zbirPromPot:N2}").Bold().FontSize(8).AlignRight();
                        table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text($"{zbirUkDug:N2}").Bold().FontSize(8).AlignRight();
                        table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text($"{zbirUkPot:N2}").Bold().FontSize(8).AlignRight();
                        table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text($"{zbirSalDug:N2}").Bold().FontSize(8).AlignRight();
                        table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text($"{zbirSalPot:N2}").Bold().FontSize(8).AlignRight();
                    });

                    col.Item().PaddingTop(30).Row(row =>
                    {
                        row.RelativeItem().Text("OBRAČUNAO : ____________________").FontSize(8);
                        row.RelativeItem().AlignCenter().Text("ŠEF RAČUNOVODSTVA : ____________________").FontSize(8);
                        row.RelativeItem().AlignRight().Text("RUKOVODILAC : ____________________").FontSize(8);
                    });
                });


                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }


    public static byte[] GenerisiBrutoBilansAnalitikePdf(Firma firma, List<BrutoBilansAnalitikeRed> redovi)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Text(firma.Naziv).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                    col.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"} | Žiro: {firma.ZiroRacun ?? "---"}").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(10).Text("BRUTO BILANS ANALITIKE (PARTNERI)").Bold().FontSize(16).AlignCenter();
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(100);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Šifra").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Partner").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Duguje").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Potražuje").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Saldo").Bold().AlignRight();
                        });

                        decimal zbirDuguje = 0, zbirPotrazuje = 0, zbirSaldo = 0;

                        foreach (var r in redovi)
                        {
                            zbirDuguje += r.Duguje;
                            zbirPotrazuje += r.Potrazuje;
                            zbirSaldo += r.Saldo;

                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(r.SifraPartnera);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(r.NazivPartnera);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{r.Duguje:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{r.Potrazuje:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{r.Saldo:N2}").AlignRight();
                        }

                        table.Cell().ColumnSpan(2).Padding(6).Text("UKUPNO:").Bold().AlignRight();
                        table.Cell().Padding(6).Text($"{zbirDuguje:N2}").Bold().AlignRight();
                        table.Cell().Padding(6).Text($"{zbirPotrazuje:N2}").Bold().AlignRight();
                        table.Cell().Padding(6).Text($"{zbirSaldo:N2}").Bold().AlignRight();
                    });

                    if (redovi.Count == 0)
                    {
                        col.Item().PaddingTop(20).AlignCenter().Text("Nema proknjiženih naloga sa dodeljenim partnerom.").FontColor(Colors.Grey.Medium).Italic();
                    }
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiNalogePdf(Firma firma, List<Nalog> nalozi)
    {
        return Document.Create(container =>
        {
            foreach (var nalog in nalozi)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(firma.Naziv).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                        col.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"} | Žiro: {firma.ZiroRacun ?? "---"}").FontSize(9).FontColor(Colors.Grey.Medium);
                        col.Item().PaddingTop(10).Text($"NALOG ZA KNJIŽENJE br. {nalog.BrojNaloga}").Bold().FontSize(16).AlignCenter();
                        
                        string statusText = nalog.IsKnjizen ? "PROKNJIŽEN" : "NACRT";
                        col.Item().PaddingTop(3).Text($"Datum: {nalog.DatumNaloga:dd.MM.yyyy}   |   Vrsta: {nalog.VrstaNaloga ?? "Finansijski"}   |   Status: {statusText}").FontSize(10).AlignCenter().FontColor(Colors.Grey.Darken2);
                        
                        if (!string.IsNullOrWhiteSpace(nalog.Opis))
                        {
                            col.Item().PaddingTop(3).Text($"Opis: {nalog.Opis}").FontSize(10).AlignCenter().Italic();
                        }
                        
                        col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(40);  // R.br
                                columns.ConstantColumn(80);  // Konto
                                columns.RelativeColumn(3);   // Dokument / Opis
                                columns.ConstantColumn(100); // Duguje
                                columns.ConstantColumn(100); // Potražuje
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("R.br.").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Konto").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Dokument / Opis").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Duguje (RSD)").Bold().AlignRight();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Potražuje (RSD)").Bold().AlignRight();
                            });

                            decimal zbirDuguje = 0;
                            decimal zbirPotrazuje = 0;
                            int rbr = 1;

                            foreach (var st in nalog.Stavke)
                            {
                                zbirDuguje += st.Duguje;
                                zbirPotrazuje += st.Potrazuje;

                                int displayRbr = st.RedniBroj > 0 ? st.RedniBroj : rbr++;
                                string tekstDokumentOpis = !string.IsNullOrWhiteSpace(st.BrojDokumenta) && !string.IsNullOrWhiteSpace(st.Opis) && !st.BrojDokumenta.Equals(st.Opis, StringComparison.OrdinalIgnoreCase)
                                    ? $"{st.BrojDokumenta} — {st.Opis}"
                                    : (!string.IsNullOrWhiteSpace(st.BrojDokumenta) ? st.BrojDokumenta : (st.Opis ?? nalog.Opis ?? ""));

                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(displayRbr.ToString());
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(st.BrojKonta ?? "");
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text(tekstDokumentOpis);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text($"{st.Duguje:N2}").AlignRight();
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text($"{st.Potrazuje:N2}").AlignRight();
                            }

                            table.Cell().ColumnSpan(3).PaddingVertical(3).PaddingHorizontal(4).Text("UKUPNO NALOG:").Bold().AlignRight();
                            table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text($"{zbirDuguje:N2}").Bold().AlignRight();
                            table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text($"{zbirPotrazuje:N2}").Bold().AlignRight();
                        });

                        col.Item().PaddingTop(40).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Nalog izradio:").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().PaddingTop(25).LineHorizontal(0.5f).LineColor(Colors.Grey.Darken1);
                            });
                            row.ConstantItem(40);
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Nalog proknjižio:").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().PaddingTop(25).LineHorizontal(0.5f).LineColor(Colors.Grey.Darken1);
                            });
                            row.ConstantItem(40);
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Odobrio / Kontrolisao:").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().PaddingTop(25).LineHorizontal(0.5f).LineColor(Colors.Grey.Darken1);
                            });
                        });
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("Stranica ");
                        x.CurrentPageNumber();
                        x.Span(" od ");
                        x.TotalPages();
                    });
                });
            }
        }).GeneratePdf();
    }

    public static byte[] GenerisiKontniPlanPdf(Firma firma, List<Konto> konta)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Text(firma.Naziv).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                    col.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"} | Žiro: {firma.ZiroRacun ?? "---"}").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(10).Text("KONTNI PLAN").Bold().FontSize(16).AlignCenter();
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(90);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(100);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Konto").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Naziv konta").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Klasa").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Tip").Bold();
                        });

                        foreach (var k in konta)
                        {
                            var textBroj = table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(k.BrojKonta);
                            if (k.IsSintetika) textBroj.Bold();

                            var textNaziv = table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(k.NazivKonta);
                            if (k.IsSintetika) textNaziv.Bold();

                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"Klasa {k.Klasa}");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(k.IsSintetika ? "Sintetički" : "Analitički");
                        }
                    });
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiRacunOtpremnicuPdf(Firma firma, RacunOtpremnica racun)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text(firma.Naziv).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                            c.Item().Text($"{firma.Adresa}, {firma.PttIMesto}");
                            c.Item().Text($"PIB: {firma.Pib ?? "---"} | MB: {firma.MaticniBroj ?? "---"}");
                            c.Item().Text($"Žiro račun: {firma.ZiroRacun ?? "---"}");
                        });
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"KUPAC:").Bold().FontSize(10).FontColor(Colors.Grey.Darken2);
                            c.Item().Text(racun.Partner?.Naziv ?? "---").Bold().FontSize(12);
                            c.Item().Text($"{racun.Partner?.Adresa}, {racun.Partner?.PttIMesto}");
                            c.Item().Text($"PIB: {racun.Partner?.Pib ?? "---"}");
                        });
                    });

                    col.Item().PaddingTop(15).Text($"FAKTURA / RAČUN-OTPREMNICA br. {racun.BrojRacuna}").Bold().FontSize(16).AlignCenter();
                    col.Item().PaddingTop(3).Text($"Datum izdavanja: {racun.DatumRacuna:dd.MM.yyyy}   |   Rok dospelosti: {racun.RokPlacanja?.ToString("dd.MM.yyyy") ?? "---"}").FontSize(10).AlignCenter().FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);  // R.br
                            columns.RelativeColumn(3);   // Naziv artikla
                            columns.ConstantColumn(50);  // Kol.
                            columns.ConstantColumn(65);  // Cena
                            columns.ConstantColumn(50);  // Rabat
                            columns.ConstantColumn(65);  // Osnovica
                            columns.ConstantColumn(45);  // PDV%
                            columns.ConstantColumn(70);  // Ukupno
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(3).Text("R.br").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(3).Text("Artikal / Roba").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(3).Text("Količina").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(3).Text("Cena").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(3).Text("Rabat%").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(3).Text("Osnovica").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(3).Text("PDV%").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(3).Text("Ukupno").Bold().AlignRight();
                        });

                        int rbr = 1;
                        foreach (var st in racun.Stavke)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(3).Text(rbr++.ToString());
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(3).Text(st.Artikal?.Naziv ?? "---");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(3).Text($"{st.Kolicina:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(3).Text($"{st.ProdajnaCena:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(3).Text($"{st.RabatProcenat:N0}%").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(3).Text($"{st.Osnovica:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(3).Text($"{st.StopaPdv:N0}%").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(3).Text($"{st.Ukupno:N2}").AlignRight();
                        }
                    });

                    col.Item().PaddingTop(15).Row(row =>
                    {
                        row.RelativeItem(2).Column(c =>
                        {
                            if (!string.IsNullOrWhiteSpace(racun.Napomena))
                            {
                                c.Item().Text($"Napomena: {racun.Napomena}").Italic().FontSize(9);
                            }
                            c.Item().PaddingTop(10).Text("Oslobođeno PDV-a po članu: ---").FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                        row.RelativeItem(2).Column(c =>
                        {
                            c.Item().Row(r => { r.RelativeItem().Text("Ukupno osnovica:"); r.RelativeItem().Text($"{racun.UkupnoOsnovica:N2} RSD").Bold().AlignRight(); });
                            c.Item().Row(r => { r.RelativeItem().Text("Ukupno PDV:"); r.RelativeItem().Text($"{racun.UkupnoPdv:N2} RSD").Bold().AlignRight(); });
                            c.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(4).Row(r => { r.RelativeItem().Text("ZA UPLATU:").Bold().FontSize(11); r.RelativeItem().Text($"{racun.UkupnoZaUplatu:N2} RSD").Bold().FontSize(12).FontColor(Colors.Blue.Darken2).AlignRight(); });
                        });
                    });

                    col.Item().PaddingTop(40).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Fakturisao:").FontSize(9);
                            c.Item().PaddingTop(25).LineHorizontal(0.5f).LineColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(40);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Robu primio:").FontSize(9);
                            c.Item().PaddingTop(25).LineHorizontal(0.5f).LineColor(Colors.Grey.Darken1);
                        });
                    });
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiNivelacijuPdf(Firma firma, NivelacijaCena nivelacija)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Text(firma.Naziv).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                    col.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"} | Žiro: {firma.ZiroRacun ?? "---"}").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(10).Text($"ZAPISNIK O NIVELACIJI CENA br. {nivelacija.BrojNivelacije}").Bold().FontSize(16).AlignCenter();
                    col.Item().PaddingTop(3).Text($"Datum: {nivelacija.DatumNivelacije:dd.MM.yyyy}   |   Magacin: {nivelacija.Magacin?.NazivMagacina ?? "---"}").FontSize(10).AlignCenter().FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(80);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("R.br").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Artikal / Roba").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Zaliha").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Stara cena").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Nova cena").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Razlika/jed.").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Uk. razlika").Bold().AlignRight();
                        });

                        int rbr = 1;
                        foreach (var st in nivelacija.Stavke)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(rbr++.ToString());
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(st.Artikal?.Naziv ?? "---");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{st.KolicinaZaliha:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{st.StaraCena:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{st.NovaCena:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{st.RazlikaPoJedinici:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{st.UkupnaRazlika:N2}").AlignRight();
                        }

                        table.Cell().ColumnSpan(6).Padding(5).Text("UKUPNA RAZLIKA NIVELACIJE:").Bold().AlignRight();
                        table.Cell().Padding(5).Text($"{nivelacija.UkupnoRazlika:N2} RSD").Bold().AlignRight();
                    });
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiBilansStanjaPdf(Firma firma, List<BilansPozicija> pozicije, DateTime datum)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Text(firma.Naziv).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                    col.Item().Text($"PIB: {firma.Pib} | MB: {firma.MaticniBroj} | Mesto: {firma.PttIMesto}").FontSize(9).FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(5).Text($"BILANS STANJA na datum {datum:dd.MM.yyyy}. godine").Bold().FontSize(16).AlignCenter();
                    col.Item().Text("(Iznosi su iskazani u RSD po AOP pozicijama APR-a)").FontSize(9).Italic().AlignCenter();
                    col.Item().PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(45);  // AOP
                        cols.RelativeColumn();    // Naziv pozicije
                        cols.ConstantColumn(60);  // Konta
                        cols.ConstantColumn(110); // Iznos Tekuća godina
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("AOP").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("POZICIJA BILANSA STANJA").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Konta").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Iznos (RSD)").Bold().AlignRight();
                    });

                    foreach (var p in pozicije)
                    {
                        bool isBold = p.TipPozicije != TipPozicijeBilansa.AopStavka;
                        var bgColor = p.TipPozicije switch
                        {
                            TipPozicijeBilansa.Naslov => Colors.Grey.Lighten2,
                            TipPozicijeBilansa.Ukupno => Colors.Grey.Lighten4,
                            _ => Colors.White
                        };

                        var cellAop = table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4);
                        if (isBold) cellAop.Text(p.AopCode).Bold(); else cellAop.Text(p.AopCode);

                        var cellNaziv = table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4);
                        if (isBold) cellNaziv.Text(p.Naziv).Bold(); else cellNaziv.Text(p.Naziv);

                        table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(p.OpsegKonta);

                        var cellIznos = table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight();
                        string iznosTxt = p.TipPozicije == TipPozicijeBilansa.Naslov ? "" : $"{p.IznosTekucaGodina:N2}";
                        if (isBold) cellIznos.Text(iznosTxt).Bold(); else cellIznos.Text(iznosTxt);
                    }
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiBilansUspehaPdf(Firma firma, List<BilansPozicija> pozicije, DateTime? odDatuma, DateTime? doDatuma)
    {
        string periodTxt = (odDatuma.HasValue && doDatuma.HasValue)
            ? $"za period od {odDatuma:dd.MM.yyyy}. do {doDatuma:dd.MM.yyyy}. godine"
            : "za tekuću poslovnu godinu";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Text(firma.Naziv).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                    col.Item().Text($"PIB: {firma.Pib} | MB: {firma.MaticniBroj} | Mesto: {firma.PttIMesto}").FontSize(9).FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(5).Text($"BILANS USPEHA {periodTxt}").Bold().FontSize(16).AlignCenter();
                    col.Item().Text("(Iznosi su iskazani u RSD po AOP pozicijama APR-a)").FontSize(9).Italic().AlignCenter();
                    col.Item().PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(45);  // AOP
                        cols.RelativeColumn();    // Naziv pozicije
                        cols.ConstantColumn(60);  // Konta
                        cols.ConstantColumn(110); // Iznos Tekuća godina
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("AOP").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("POZICIJA BILANSA USPEHA").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Konta").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Iznos (RSD)").Bold().AlignRight();
                    });

                    foreach (var p in pozicije)
                    {
                        bool isBold = p.TipPozicije != TipPozicijeBilansa.AopStavka;
                        var bgColor = p.TipPozicije switch
                        {
                            TipPozicijeBilansa.Naslov => Colors.Grey.Lighten2,
                            TipPozicijeBilansa.Ukupno => Colors.Grey.Lighten4,
                            _ => Colors.White
                        };

                        var cellAop = table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4);
                        if (isBold) cellAop.Text(p.AopCode).Bold(); else cellAop.Text(p.AopCode);

                        var cellNaziv = table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4);
                        if (isBold) cellNaziv.Text(p.Naziv).Bold(); else cellNaziv.Text(p.Naziv);

                        table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(p.OpsegKonta);

                        var cellIznos = table.Cell().Background(bgColor).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight();
                        string iznosTxt = p.TipPozicije == TipPozicijeBilansa.Naslov ? "" : $"{p.IznosTekucaGodina:N2}";
                        if (isBold) cellIznos.Text(iznosTxt).Bold(); else cellIznos.Text(iznosTxt);
                    }
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiKirPdf(Firma firma, List<PdvZapis> zapisi, DateTime? odDatuma, DateTime? doDatuma)
    {
        string periodTxt = (odDatuma.HasValue && doDatuma.HasValue) ? $"{odDatuma:dd.MM.yyyy} - {doDatuma:dd.MM.yyyy}" : "Svi zapisi";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Text(firma.Naziv).Bold().FontSize(13).FontColor(Colors.Blue.Medium);
                    col.Item().Text($"PIB: {firma.Pib} | MB: {firma.MaticniBroj} | Mesto: {firma.PttIMesto}").FontSize(8).FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(4).Text($"KNJIGA IZDATIH RAČUNA (KIR) — Period: {periodTxt}").Bold().FontSize(14).AlignCenter();
                    col.Item().PaddingBottom(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(30);  // Rbr
                        cols.ConstantColumn(60);  // Datum
                        cols.ConstantColumn(75);  // Broj racuna
                        cols.RelativeColumn();    // Kupac
                        cols.ConstantColumn(65);  // PIB
                        cols.ConstantColumn(80);  // Ukupno sa PDV
                        cols.ConstantColumn(75);  // Osnovica 20%
                        cols.ConstantColumn(65);  // PDV 20%
                        cols.ConstantColumn(75);  // Osnovica 10%
                        cols.ConstantColumn(65);  // PDV 10%
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Rbr").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Datum").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Br. računa").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Kupac").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("PIB").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Ukupno").Bold().AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Osn. 20%").Bold().AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("PDV 20%").Bold().AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Osn. 10%").Bold().AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("PDV 10%").Bold().AlignRight();
                    });

                    foreach (var z in zapisi)
                    {
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(z.RedniBroj.ToString());
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(z.DatumRacuna.ToString("dd.MM.yyyy"));
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(z.BrojDokumenta);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(z.PartnerNaziv);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(z.PartnerPib);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{z.UkupnaNaknadaSaPdv:N2}").AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{z.Osnovica20:N2}").AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{z.Pdv20:N2}").AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{z.Osnovica10:N2}").AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{z.Pdv10:N2}").AlignRight();
                    }

                    table.Cell().ColumnSpan(5).Padding(4).Text("UKUPNO KIR:").Bold().AlignRight();
                    table.Cell().Padding(4).Text($"{zapisi.Sum(x => x.UkupnaNaknadaSaPdv):N2}").Bold().AlignRight();
                    table.Cell().Padding(4).Text($"{zapisi.Sum(x => x.Osnovica20):N2}").Bold().AlignRight();
                    table.Cell().Padding(4).Text($"{zapisi.Sum(x => x.Pdv20):N2}").Bold().AlignRight();
                    table.Cell().Padding(4).Text($"{zapisi.Sum(x => x.Osnovica10):N2}").Bold().AlignRight();
                    table.Cell().Padding(4).Text($"{zapisi.Sum(x => x.Pdv10):N2}").Bold().AlignRight();
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiKprPdf(Firma firma, List<PdvZapis> zapisi, DateTime? odDatuma, DateTime? doDatuma)
    {
        string periodTxt = (odDatuma.HasValue && doDatuma.HasValue) ? $"{odDatuma:dd.MM.yyyy} - {doDatuma:dd.MM.yyyy}" : "Svi zapisi";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Text(firma.Naziv).Bold().FontSize(13).FontColor(Colors.Blue.Medium);
                    col.Item().Text($"PIB: {firma.Pib} | MB: {firma.MaticniBroj} | Mesto: {firma.PttIMesto}").FontSize(8).FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(4).Text($"KNJIGA PRIMLJENIH RAČUNA (KPR) — Period: {periodTxt}").Bold().FontSize(14).AlignCenter();
                    col.Item().PaddingBottom(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(30);  // Rbr
                        cols.ConstantColumn(60);  // Datum
                        cols.ConstantColumn(75);  // Broj racuna
                        cols.RelativeColumn();    // Dobavljac
                        cols.ConstantColumn(65);  // PIB
                        cols.ConstantColumn(80);  // Ukupno sa PDV
                        cols.ConstantColumn(75);  // Osnovica 20%
                        cols.ConstantColumn(65);  // Prethodni PDV 20%
                        cols.ConstantColumn(75);  // Osnovica 10%
                        cols.ConstantColumn(65);  // Prethodni PDV 10%
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Rbr").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Datum").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Br. računa").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Dobavljač").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("PIB").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Ukupno").Bold().AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Osn. 20%").Bold().AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Preth.PDV 20%").Bold().AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Osn. 10%").Bold().AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Preth.PDV 10%").Bold().AlignRight();
                    });

                    foreach (var z in zapisi)
                    {
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(z.RedniBroj.ToString());
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(z.DatumRacuna.ToString("dd.MM.yyyy"));
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(z.BrojDokumenta);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(z.PartnerNaziv);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(z.PartnerPib);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{z.UkupnaNaknadaSaPdv:N2}").AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{z.Osnovica20:N2}").AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{z.Pdv20:N2}").AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{z.Osnovica10:N2}").AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{z.Pdv10:N2}").AlignRight();
                    }

                    table.Cell().ColumnSpan(5).Padding(4).Text("UKUPNO KPR:").Bold().AlignRight();
                    table.Cell().Padding(4).Text($"{zapisi.Sum(x => x.UkupnaNaknadaSaPdv):N2}").Bold().AlignRight();
                    table.Cell().Padding(4).Text($"{zapisi.Sum(x => x.Osnovica20):N2}").Bold().AlignRight();
                    table.Cell().Padding(4).Text($"{zapisi.Sum(x => x.Pdv20):N2}").Bold().AlignRight();
                    table.Cell().Padding(4).Text($"{zapisi.Sum(x => x.Osnovica10):N2}").Bold().AlignRight();
                    table.Cell().Padding(4).Text($"{zapisi.Sum(x => x.Pdv10):N2}").Bold().AlignRight();
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiSifrarnikRacunopolagacaPdf(Firma firma, List<Magacin> magacini)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Text(firma.Naziv).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                    col.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"}").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(10).Text("ŠIFRARNIK RAČUNOPOLAGAČA (MAGACINA)").Bold().FontSize(16).AlignCenter();
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(40);
                            columns.ConstantColumn(70);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.ConstantColumn(100);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("R.br").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Šifra").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Naziv / Računopolagač").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Odgovorno lice").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Vrsta").Bold();
                        });

                        int rbr = 1;
                        foreach (var m in magacini)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(4).Text(rbr.ToString());
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(4).Text(m.SifraMagacina).Bold();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(4).Text(m.NazivMagacina);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(4).Text(m.OdgovornoLice ?? "---");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(4).Text(m.VrstaMagacina ?? "Veleprodaja");
                            rbr++;
                        }
                    });
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiSifrarnikArtikalaPdf(Firma firma, List<Artikal> artikli)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Text(firma.Naziv).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                    col.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"}").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(10).Text("ŠIFRARNIK ARTIKALA I ROBE").Bold().FontSize(16).AlignCenter();
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(35);
                            columns.ConstantColumn(65);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(45);
                            columns.ConstantColumn(65);
                            columns.ConstantColumn(65);
                            columns.ConstantColumn(75);
                            columns.ConstantColumn(75);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("R.br").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Šifra").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Naziv artikla").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("J.M.").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Pakovanje").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Tar.broj").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Nab. cena").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Prod. cena").Bold().AlignRight();
                        });

                        int rbr = 1;
                        foreach (var a in artikli)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(rbr.ToString());
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(a.SifraArtikla).Bold();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(a.Naziv);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(a.JedinicaMere ?? "kom");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(a.Pakovanje ?? "---");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(a.TarifniBroj ?? "---");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text($"{a.NabavnaCena:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text($"{a.ProdajnaCena:N2}").AlignRight();
                            rbr++;
                        }
                    });
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiSifrarnikMaterijalaPdf(Firma firma, List<Materijal> materijali)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Text(firma.Naziv).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                    col.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"}").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(10).Text("ŠIFARNIK MATERIJALA").Bold().FontSize(16).AlignCenter();
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(35);
                            columns.ConstantColumn(65);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(45);
                            columns.ConstantColumn(90);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("R.br").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Šifra").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Naziv materijala").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("J.M.").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Pakovanje").Bold();
                        });

                        int rbr = 1;
                        foreach (var m in materijali)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(rbr.ToString());
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(m.SifraArtikla).Bold();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(m.Naziv);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(m.JedinicaMere ?? "kom");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(m.Pakovanje ?? "---");
                            rbr++;
                        }
                    });
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiSifrarnikPoreskihTarifaPdf(Firma firma, List<PoreskaTarifa> tarife)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Text(firma.Naziv).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                    col.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"}").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(10).Text("ŠIFARNIK PORESKIH TARIFA").Bold().FontSize(16).AlignCenter();
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(40);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(90);
                            columns.ConstantColumn(100);
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("R.br").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Tar. br.").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Porez").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Poseban porez").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Porez u ceni").Bold();
                        });

                        int rbr = 1;
                        foreach (var t in tarife)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(rbr.ToString());
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(t.TarifniBroj).Bold();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text($"{t.PorezProcenat:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text($"{t.PosebanPorezProcenat:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(t.PorezUCeni ? "DA" : "NE");
                            rbr++;
                        }
                    });
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiPrimopredajuPdf(Firma firma, PrimopredajaNalog nalog, Magacin magDaje, Magacin magPrima)
    {
        bool jeZaduzenje = nalog.VrstaDokumenta.StartsWith("Zadu", StringComparison.OrdinalIgnoreCase);
        bool jeRazduzenje = nalog.VrstaDokumenta.StartsWith("Razdu", StringComparison.OrdinalIgnoreCase);
        bool jednostrano = jeZaduzenje || jeRazduzenje;
        // Zaduženje: magacin koji se zadužuje (duguje) je SifraMagacinaPrima.
        // Razduženje: magacin koji se razdužuje (potražuje) je SifraMagacinaDaje.
        var jedanMagacin = jeZaduzenje ? magPrima : magDaje;
        string naslov = jeZaduzenje ? $"Zaduženje br. {nalog.BrojNaloga}"
            : jeRazduzenje ? $"Razduženje br. {nalog.BrojNaloga}"
            : $"NALOG ZA PRIMOPREDAJU ROBE Br. {nalog.BrojNaloga}";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(firma.Naziv).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                            c.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"}").FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                        row.ConstantItem(150).AlignRight().Text($"Datum štampe: {DateTime.Now:dd.MM.yyyy}").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                    col.Item().PaddingTop(10).Text(naslov).Bold().FontSize(16).AlignCenter();
                    col.Item().Text($"Datum naloga: {nalog.Datum:dd.MM.yyyy}").FontSize(10).AlignCenter();
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    if (jednostrano)
                    {
                        col.Item().Text($"{(jeZaduzenje ? "Duguje" : "Potražuje")}: magacin {jedanMagacin.SifraMagacina}").Bold().FontSize(11);
                        col.Item().Text($"Naziv: {jedanMagacin.NazivMagacina}");
                    }
                    else
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("IZDAJE (MAGACIN DAJE):").Bold().FontSize(11);
                                c.Item().Text($"Šifra i naziv: {magDaje.SifraMagacina} - {magDaje.NazivMagacina}");
                                c.Item().Text($"Odgovorno lice: {magDaje.OdgovornoLice ?? "---"}");
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("PRIMA (MAGACIN PRIMA):").Bold().FontSize(11);
                                c.Item().Text($"Šifra i naziv: {magPrima.SifraMagacina} - {magPrima.NazivMagacina}");
                                c.Item().Text($"Odgovorno lice: {magPrima.OdgovornoLice ?? "---"}");
                            });
                        });
                    }

                    col.Item().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(35);
                            columns.ConstantColumn(70);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(35);
                            columns.ConstantColumn(65);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(90);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("R.br").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Šifra").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Naziv artikla").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("J.M.").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Količina").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Cena").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Vrednost").Bold().AlignRight();
                        });

                        int rbr = 1;
                        decimal ukupnoVrednost = 0m;
                        foreach (var st in nalog.Stavke)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(rbr.ToString());
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(st.SifraArtikla).Bold();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(st.NazivArtikla ?? st.SifraArtikla);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text(st.JedinicaMere ?? "");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text($"{st.Kolicina:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text($"{st.Cena:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text($"{st.Iznos:N2}").AlignRight();
                            ukupnoVrednost += st.Iznos;
                            rbr++;
                        }

                        table.Cell().ColumnSpan(6).PaddingVertical(6).PaddingHorizontal(4).Text("TOTAL:").Bold().AlignRight();
                        table.Cell().PaddingVertical(6).PaddingHorizontal(4).Text($"{ukupnoVrednost:N2}").Bold().AlignRight();
                    });

                    if (jednostrano)
                    {
                        col.Item().PaddingTop(40).Text(jeZaduzenje ? "Robu prima:" : "Robu predaje:").FontSize(9);
                        col.Item().PaddingTop(20).Width(200).Text("_______________________").FontSize(9);
                    }
                    else
                    {
                        col.Item().PaddingTop(40).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Robu predao:").FontSize(9);
                                c.Item().PaddingTop(20).Text("_______________________").FontSize(9);
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Robu primio:").FontSize(9);
                                c.Item().PaddingTop(20).Text("_______________________").FontSize(9);
                            });
                        });
                    }
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    /// <summary>Generiše PDF Kalkulacije cena na veliko (MAT6 - kal_nal), sa stavkama po artiklu.</summary>
    public static byte[] GenerisiKalkulacijuPdf(Firma firma, Kalkulacija kalk, Partner? dobavljac, Magacin? magacin)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(7).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Text(firma.Naziv).Bold().FontSize(12);
                    col.Item().Text(firma.Adresa).FontSize(8).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(6).Text($"KALKULACIJA CENA NA VELIKO broj {kalk.BrojKalkulacije} od {kalk.Datum:dd.MM.yyyy}").Bold().FontSize(12).AlignCenter();

                    col.Item().PaddingTop(6).Text($"Dobavljač: {kalk.SifraDobavljaca}   {dobavljac?.Naziv ?? ""}").FontSize(8);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Otpremnica-prijemnica br. {kalk.BrojOtpremnice}   Datum {(kalk.DatumOtpremnice.HasValue ? kalk.DatumOtpremnice.Value.ToString("dd.MM.yyyy") : "")}").FontSize(8);
                        row.RelativeItem().Text($"RAČUN: {kalk.BrojRacuna}   datum {(kalk.DatumRacuna.HasValue ? kalk.DatumRacuna.Value.ToString("dd.MM.yyyy") : "")}").FontSize(8);
                    });

                    col.Item().PaddingTop(4).AlignRight().Width(230).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(90); });
                        t.Cell().Text("Fakturna vrednost:");
                        t.Cell().AlignRight().Text($"{kalk.Stavke.Sum(s => s.Iznos):N2}");
                        t.Cell().Text("Svega nabavna vrednost:");
                        t.Cell().AlignRight().Text($"{kalk.SvegaNabavno:N2}");
                        t.Cell().Text("Razlika u ceni:");
                        t.Cell().AlignRight().Text($"{kalk.Razlika:N2}");
                        t.Cell().Text("PDV:");
                        t.Cell().AlignRight().Text($"{kalk.Porez:N2}");
                        t.Cell().Text("Prodajna vrednost:").Bold();
                        t.Cell().AlignRight().Text($"{kalk.ProdajnaVrednost:N2}").Bold();
                    });

                    if (magacin != null)
                        col.Item().PaddingTop(4).Text($"Magacin {magacin.SifraMagacina}   račun opolagač   {magacin.NazivMagacina}").FontSize(8);

                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(6).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(18);   // Rbr
                        columns.ConstantColumn(35);   // Šifra
                        columns.RelativeColumn(2.2f); // Naziv
                        columns.ConstantColumn(20);   // J.M.
                        columns.ConstantColumn(42);   // Količina
                        columns.ConstantColumn(42);   // Nabavna cena
                        columns.ConstantColumn(45);   // Iznos
                        columns.ConstantColumn(38);   // Troškovi
                        columns.ConstantColumn(48);   // Nabavna vrednost
                        columns.ConstantColumn(30);   // Razlika %
                        columns.ConstantColumn(42);   // Iznos razlike
                        columns.ConstantColumn(48);   // Vrednost bez poreza
                        columns.ConstantColumn(26);   // PDV %
                        columns.ConstantColumn(40);   // Iznos PDV-a
                        columns.ConstantColumn(48);   // Prodajna vrednost
                        columns.ConstantColumn(40);   // Cena po J.M.
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("R.br").Bold().FontSize(6.5f);
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Šifra").Bold().FontSize(6.5f);
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Naziv artikla").Bold().FontSize(6.5f);
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("J.M.").Bold().FontSize(6.5f);
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Količina").Bold().FontSize(6.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Nabavna cena").Bold().FontSize(6.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Iznos").Bold().FontSize(6.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Troškovi").Bold().FontSize(6.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Nabavna vrednost").Bold().FontSize(6.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Razlika %").Bold().FontSize(6.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Iznos razlike").Bold().FontSize(6.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Vred. bez poreza").Bold().FontSize(6.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("PDV %").Bold().FontSize(6.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Iznos PDV-a").Bold().FontSize(6.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Prodajna vrednost").Bold().FontSize(6.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Cena po J.M.").Bold().FontSize(6.5f).AlignRight();
                    });

                    int rbr = 1;
                    foreach (var st in kalk.Stavke)
                    {
                        decimal vrednostBezPoreza = st.NabavnaVrednost + st.RazlikaIznos;
                        decimal razlikaProcenat = kalk.MarzaProcenat;
                        decimal pdvProcenat = kalk.PoreskaStopaProcenat;

                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(rbr.ToString()).FontSize(6.5f);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(st.SifraArtikla).Bold().FontSize(6.5f);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(st.NazivArtikla ?? st.SifraArtikla).FontSize(6.5f);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(st.JedinicaMere ?? "").FontSize(6.5f);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.Kolicina:N2}").FontSize(6.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.NabavnaCena:N2}").FontSize(6.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.Iznos:N2}").FontSize(6.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.Troskovi:N2}").FontSize(6.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.NabavnaVrednost:N2}").FontSize(6.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{razlikaProcenat:N2}").FontSize(6.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.RazlikaIznos:N2}").FontSize(6.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{vrednostBezPoreza:N2}").FontSize(6.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{pdvProcenat:N2}").FontSize(6.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.PorezIznos:N2}").FontSize(6.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.ProdajnaVrednost:N2}").FontSize(6.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.ProdajnaCena:N2}").FontSize(6.5f).AlignRight();
                        rbr++;
                    }

                    table.Cell().ColumnSpan(8).PaddingVertical(4).PaddingHorizontal(2).Text("SVEGA:").Bold().FontSize(7).AlignRight();
                    table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text($"{kalk.Stavke.Sum(s => s.NabavnaVrednost):N2}").Bold().FontSize(6.5f).AlignRight();
                    table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text("").FontSize(6.5f);
                    table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text($"{kalk.Razlika:N2}").Bold().FontSize(6.5f).AlignRight();
                    table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text($"{kalk.Stavke.Sum(s => s.NabavnaVrednost + s.RazlikaIznos):N2}").Bold().FontSize(6.5f).AlignRight();
                    table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text("").FontSize(6.5f);
                    table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text($"{kalk.Porez:N2}").Bold().FontSize(6.5f).AlignRight();
                    table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text($"{kalk.ProdajnaVrednost:N2}").Bold().FontSize(6.5f).AlignRight();
                    table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text("").FontSize(6.5f);
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ").FontSize(7);
                    x.CurrentPageNumber().FontSize(7);
                    x.Span(" od ").FontSize(7);
                    x.TotalPages().FontSize(7);
                });
            });
        }).GeneratePdf();
    }

    /// <summary>
    /// Generiše PDF Kalkulacije cena na malo (MAT3/MAT7 - mal_nal, stampaMkalkulacija() iz MAT2.PRG).
    /// Ako kalkulacija ima stavke, štampa se stavka-po-stavka tabela (analogno veleprodajnoj
    /// <see cref="GenerisiKalkulacijuPdf"/>); starije/legacy-uvezene kalkulacije bez stavki i dalje
    /// dobijaju header-only prikaz kao ranije.
    /// </summary>
    public static byte[] GenerisiMaloprodajnuKalkulacijuPdf(Firma firma, MaloprodajnaKalkulacija kalk, Partner? dobavljac, Magacin? magacinDaje, Magacin? magacinPrima)
    {
        if (kalk.Stavke.Count == 0)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(firma.Naziv).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                        col.Item().Text(firma.Adresa).FontSize(9).FontColor(Colors.Grey.Medium);
                        col.Item().PaddingTop(10).Text($"KALKULACIJA CENA NA MALO broj {kalk.BrojKalkulacije} od {kalk.Datum:dd.MM.yyyy}").Bold().FontSize(14).AlignCenter();
                        col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingVertical(12).Column(col =>
                    {
                        col.Item().Text($"Dobavljač: {kalk.SifraDobavljaca}   {dobavljac?.Naziv ?? ""}").FontSize(10);
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Otpremnica-prijemnica br. {kalk.BrojOtpremnice}   Datum {(kalk.DatumOtpremnice.HasValue ? kalk.DatumOtpremnice.Value.ToString("dd.MM.yyyy") : "")}").FontSize(10);
                            row.RelativeItem().Text($"RAČUN: {kalk.BrojRacuna}   datum {(kalk.DatumRacuna.HasValue ? kalk.DatumRacuna.Value.ToString("dd.MM.yyyy") : "")}").FontSize(10);
                        });

                        if (magacinPrima != null)
                            col.Item().PaddingTop(6).Text($"Magacin (prima): {magacinPrima.SifraMagacina} - {magacinPrima.NazivMagacina}").FontSize(10);
                        if (magacinDaje != null)
                            col.Item().Text($"Magacin (daje): {magacinDaje.SifraMagacina} - {magacinDaje.NazivMagacina}").FontSize(10);

                        col.Item().PaddingTop(15).AlignRight().Width(260).Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(100); });
                            t.Cell().Text("Svega nabavna vrednost:");
                            t.Cell().AlignRight().Text($"{kalk.SvegaNabavno:N2} RSD");
                            t.Cell().Text("Razlika u ceni:");
                            t.Cell().AlignRight().Text($"{kalk.Razlika:N2} RSD");
                            t.Cell().Text("PDV:");
                            t.Cell().AlignRight().Text($"{kalk.Porez:N2} RSD");
                            t.Cell().Text("Rabat:");
                            t.Cell().AlignRight().Text($"{kalk.RabatIznos:N2} RSD");
                            t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Prodajna vrednost:").Bold();
                            t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text($"{kalk.ProdajnaVrednost:N2} RSD").Bold().AlignRight();
                        });

                        col.Item().PaddingTop(20).Text("Napomena: kalkulacija je uvezena bez stavki po artiklu (legacy header-only zapis).")
                            .FontSize(8).Italic().FontColor(Colors.Grey.Medium);
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("Stranica ");
                        x.CurrentPageNumber();
                        x.Span(" od ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(7).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Text(firma.Naziv).Bold().FontSize(12);
                    col.Item().Text(firma.Adresa).FontSize(8).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(6).Text($"KALKULACIJA CENA NA MALO broj {kalk.BrojKalkulacije} od {kalk.Datum:dd.MM.yyyy}").Bold().FontSize(12).AlignCenter();

                    col.Item().PaddingTop(6).Text($"Dobavljač: {kalk.SifraDobavljaca}   {dobavljac?.Naziv ?? ""}").FontSize(8);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Otpremnica-prijemnica br. {kalk.BrojOtpremnice}   Datum {(kalk.DatumOtpremnice.HasValue ? kalk.DatumOtpremnice.Value.ToString("dd.MM.yyyy") : "")}").FontSize(8);
                        row.RelativeItem().Text($"RAČUN: {kalk.BrojRacuna}   datum {(kalk.DatumRacuna.HasValue ? kalk.DatumRacuna.Value.ToString("dd.MM.yyyy") : "")}").FontSize(8);
                        row.RelativeItem().Text(magacinDaje != null ? $"Magacin (daje): {magacinDaje.SifraMagacina} - {magacinDaje.NazivMagacina}" : "").FontSize(8);
                        row.RelativeItem().Text(magacinPrima != null ? $"Magacin (prima): {magacinPrima.SifraMagacina} - {magacinPrima.NazivMagacina}" : "").FontSize(8);
                    });

                    col.Item().PaddingTop(4).AlignRight().Width(230).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(90); });
                        t.Cell().Text("Fakturna vrednost:");
                        t.Cell().AlignRight().Text($"{kalk.Stavke.Sum(s => s.Iznos):N2}");
                        t.Cell().Text("Svega nabavna vrednost:");
                        t.Cell().AlignRight().Text($"{kalk.SvegaNabavno:N2}");
                        t.Cell().Text("Razlika u ceni:");
                        t.Cell().AlignRight().Text($"{kalk.Razlika:N2}");
                        t.Cell().Text("PDV:");
                        t.Cell().AlignRight().Text($"{kalk.Porez:N2}");
                        t.Cell().Text("Rabat (informativno):");
                        t.Cell().AlignRight().Text($"{kalk.RabatIznos:N2}");
                        t.Cell().Text("Prodajna vrednost:").Bold();
                        t.Cell().AlignRight().Text($"{kalk.ProdajnaVrednost:N2}").Bold();
                    });

                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(6).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(18);   // Rbr
                        columns.ConstantColumn(35);   // Šifra
                        columns.RelativeColumn(2.2f); // Naziv
                        columns.ConstantColumn(20);   // J.M.
                        columns.ConstantColumn(42);   // Količina
                        columns.ConstantColumn(42);   // Nabavna cena
                        columns.ConstantColumn(45);   // Iznos
                        columns.ConstantColumn(38);   // Troškovi
                        columns.ConstantColumn(48);   // Nabavna vrednost
                        columns.ConstantColumn(30);   // Razlika %
                        columns.ConstantColumn(42);   // Iznos razlike
                        columns.ConstantColumn(48);   // Vrednost bez poreza
                        columns.ConstantColumn(26);   // PDV %
                        columns.ConstantColumn(40);   // Iznos PDV-a
                        columns.ConstantColumn(48);   // Prodajna vrednost
                        columns.ConstantColumn(40);   // Cena po J.M.
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("R.br").Bold().FontSize(6.5f);
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Šifra").Bold().FontSize(6.5f);
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Naziv artikla").Bold().FontSize(6.5f);
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("J.M.").Bold().FontSize(6.5f);
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Količina").Bold().FontSize(6.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Nabavna cena").Bold().FontSize(6.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Iznos").Bold().FontSize(6.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Troškovi").Bold().FontSize(6.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Nabavna vrednost").Bold().FontSize(6.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Razlika %").Bold().FontSize(6.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Iznos razlike").Bold().FontSize(6.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Vred. bez poreza").Bold().FontSize(6.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("PDV %").Bold().FontSize(6.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Iznos PDV-a").Bold().FontSize(6.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Prodajna vrednost").Bold().FontSize(6.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Cena po J.M.").Bold().FontSize(6.5f).AlignRight();
                    });

                    int rbr = 1;
                    foreach (var st in kalk.Stavke)
                    {
                        decimal vrednostBezPoreza = st.NabavnaVrednost + st.RazlikaIznos;
                        decimal razlikaProcenat = kalk.MarzaProcenat;
                        decimal pdvProcenat = kalk.PoreskaStopaProcenat;

                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(rbr.ToString()).FontSize(6.5f);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(st.SifraArtikla).Bold().FontSize(6.5f);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(st.NazivArtikla ?? st.SifraArtikla).FontSize(6.5f);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(st.JedinicaMere ?? "").FontSize(6.5f);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.Kolicina:N2}").FontSize(6.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.NabavnaCena:N2}").FontSize(6.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.Iznos:N2}").FontSize(6.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.Troskovi:N2}").FontSize(6.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.NabavnaVrednost:N2}").FontSize(6.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{razlikaProcenat:N2}").FontSize(6.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.RazlikaIznos:N2}").FontSize(6.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{vrednostBezPoreza:N2}").FontSize(6.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{pdvProcenat:N2}").FontSize(6.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.PorezIznos:N2}").FontSize(6.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.ProdajnaVrednost:N2}").FontSize(6.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.ProdajnaCena:N2}").FontSize(6.5f).AlignRight();
                        rbr++;
                    }

                    table.Cell().ColumnSpan(8).PaddingVertical(4).PaddingHorizontal(2).Text("SVEGA:").Bold().FontSize(7).AlignRight();
                    table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text($"{kalk.Stavke.Sum(s => s.NabavnaVrednost):N2}").Bold().FontSize(6.5f).AlignRight();
                    table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text("").FontSize(6.5f);
                    table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text($"{kalk.Razlika:N2}").Bold().FontSize(6.5f).AlignRight();
                    table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text($"{kalk.Stavke.Sum(s => s.NabavnaVrednost + s.RazlikaIznos):N2}").Bold().FontSize(6.5f).AlignRight();
                    table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text("").FontSize(6.5f);
                    table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text($"{kalk.Porez:N2}").Bold().FontSize(6.5f).AlignRight();
                    table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text($"{kalk.ProdajnaVrednost:N2}").Bold().FontSize(6.5f).AlignRight();
                    table.Cell().PaddingVertical(4).PaddingHorizontal(2).Text("").FontSize(6.5f);
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ").FontSize(7);
                    x.CurrentPageNumber().FontSize(7);
                    x.Span(" od ").FontSize(7);
                    x.TotalPages().FontSize(7);
                });
            });
        }).GeneratePdf();
    }

    /// <summary>Generiše PDF Računa - Otpremnice za kupca (stampa_rac_otp - MAT5).</summary>
    public static byte[] GenerisiRacunOtpremnicuPdf(Firma firma, RacunOtpremnica racun, Partner? partner = null)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Header().Element(header =>
                {
                    header.Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(firma.Naziv).Bold().FontSize(14);
                            col.Item().Text($"PIB: {firma.Pib} | MB: {firma.MaticniBroj}");
                            col.Item().Text($"Adresa: {firma.Adresa}, {firma.PttIMesto}");
                            if (!string.IsNullOrWhiteSpace(firma.ZiroRacun)) col.Item().Text($"Žiro račun: {firma.ZiroRacun}");
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            bool jePredracun = racun.TipDokumenta == TipRacunOtpremnice.Predracun;
                            string naslovDokumenta = jePredracun ? "PREDRAČUN" : "RAČUN - OTPREMNICA";
                            col.Item().Text($"{naslovDokumenta} br. {racun.BrojRacuna}").Bold().FontSize(14).FontColor(jePredracun ? Colors.Orange.Darken2 : Colors.Blue.Darken2);
                            col.Item().Text($"Mesto i datum izdavanja: {firma.PttIMesto ?? "Beograd"}, {racun.DatumRacuna:dd.MM.yyyy}.");
                            if (jePredracun)
                            {
                                if (racun.RokVazenjaPredracuna.HasValue) col.Item().Text($"Rok važenja predračuna: {racun.RokVazenjaPredracuna.Value:dd.MM.yyyy}.");
                            }
                            else
                            {
                                col.Item().Text($"Datum prometa: {racun.DatumOtpremnice:dd.MM.yyyy}.");
                                col.Item().Text($"Rok plaćanja: {racun.DatumRacuna.AddDays(racun.RokPlacanjaDana):dd.MM.yyyy}. ({racun.RokPlacanjaDana} dana)");
                            }
                            if (!string.IsNullOrWhiteSpace(racun.NacinPlacanja)) col.Item().Text($"Način plaćanja: {racun.NacinPlacanja}");
                        });
                    });
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("KUPAC / PRIMALAC:").Bold().FontSize(10);
                            c.Item().Text(partner?.Naziv ?? $"Konto kupca: {racun.KontoKupca}").Bold();
                            if (partner != null)
                            {
                                c.Item().Text($"PIB: {partner.Pib} | MB: {partner.MaticniBroj}");
                                c.Item().Text($"Adresa: {partner.Adresa}, {partner.PttIMesto}");
                            }
                        });
                    });

                    col.Item().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(25);  // Rbr
                            columns.ConstantColumn(70);  // Šifra
                            columns.RelativeColumn(2);   // Naziv
                            columns.ConstantColumn(35);  // J.M.
                            columns.ConstantColumn(50);  // Kol
                            columns.ConstantColumn(60);  // Cena
                            columns.ConstantColumn(40);  // Rabat
                            columns.ConstantColumn(40);  // PDV%
                            columns.ConstantColumn(65);  // Bez PDV
                            columns.ConstantColumn(70);  // Ukupno
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("R.br").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Šifra").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Naziv artikla / robe").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("J.M.").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Količina").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Cena").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Rab%").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("PDV%").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Bez PDV").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Ukupno").Bold().AlignRight();
                        });

                        int rbr = 1;
                        foreach (var st in racun.Stavke)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(rbr.ToString());
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(st.SifraArtikla).Bold();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(st.NazivArtikla ?? st.SifraArtikla);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(st.Artikal?.JedinicaMere ?? "");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.Kolicina:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.Cena:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.RabatProcenat:N0}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.PdvProcenat:N0}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.IznosBezPdv:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.UkupanIznos:N2}").AlignRight();
                            rbr++;
                        }
                    });

                    col.Item().PaddingTop(12).AlignRight().Width(260).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(100); });
                        t.Cell().Text("Ukupno osnovica bez PDV:").Bold();
                        t.Cell().Text($"{racun.IznosBezPdv:N2} RSD").AlignRight();
                        t.Cell().Text("Ukupno PDV:").Bold();
                        t.Cell().Text($"{racun.PdvIznos:N2} RSD").AlignRight();
                        t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("ZA UPLATU:").Bold().FontSize(11);
                        t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text($"{racun.UkupanIznos:N2} RSD").Bold().FontSize(11).AlignRight();
                    });

                    col.Item().PaddingTop(20).Column(c =>
                    {
                        if (racun.TipDokumenta == TipRacunOtpremnice.Predracun)
                        {
                            c.Item().Text("Ovaj predračun ne predstavlja fakturu niti obavezu plaćanja, već služi za informisanje o uslovima buduće isporuke.").FontSize(8);
                            c.Item().Text($"Plaćanje: {racun.NacinPlacanja ?? "Virman"}.").FontSize(8);
                        }
                        else
                        {
                            c.Item().Text($"Roba otpremljena uz otpremnicu broj {racun.BrojOtpremnice ?? racun.BrojRacuna.ToString()}.").FontSize(8);
                            c.Item().Text($"Plaćanje: {racun.NacinPlacanja ?? "Virman"} u roku od {racun.RokPlacanjaDana} dana od datuma prijema robe.").FontSize(8);
                        }
                        c.Item().Text("U slučaju spora nadležan je stvarno i mesno nadležni sud.").FontSize(8);
                        c.Item().Text("Ovaj dokument je punovažan bez potpisa i pečata.").FontSize(8);
                    });

                    col.Item().PaddingTop(25).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Robu izdao / Fakturisao:").Italic();
                            c.Item().PaddingTop(20).Text("_______________________");
                        });
                        r.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("Robu primio / Kupac:").Italic();
                            c.Item().PaddingTop(20).Text("_______________________");
                        });
                    });
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiPonudaPredracunPdf(Firma firma, PonudaPredracun ponuda, Partner? partner = null)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                bool jePredracun = ponuda.VrstaDokumenta == "Predračun";
                string naslovDokumenta = jePredracun ? "PREDRAČUN" : "PONUDA";

                page.Header().Element(header =>
                {
                    header.Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(firma.Naziv).Bold().FontSize(14);
                            col.Item().Text($"PIB: {firma.Pib} | MB: {firma.MaticniBroj}");
                            col.Item().Text($"Adresa: {firma.Adresa}, {firma.PttIMesto}");
                            if (!string.IsNullOrWhiteSpace(firma.ZiroRacun)) col.Item().Text($"Žiro račun: {firma.ZiroRacun}");
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text($"{naslovDokumenta} br. {ponuda.BrojDokumenta}").Bold().FontSize(14).FontColor(jePredracun ? Colors.Orange.Darken2 : Colors.Blue.Darken2);
                            col.Item().Text($"Mesto i datum izdavanja: {firma.PttIMesto ?? "Beograd"}, {ponuda.Datum:dd.MM.yyyy}.");
                            col.Item().Text($"Rok važenja: {ponuda.RokVazenja:dd.MM.yyyy}.");
                        });
                    });
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("KUPAC / PRIMALAC:").Bold().FontSize(10);
                            c.Item().Text(partner?.Naziv ?? ponuda.NazivPartnera).Bold();
                            if (partner != null)
                            {
                                c.Item().Text($"PIB: {partner.Pib} | MB: {partner.MaticniBroj}");
                                c.Item().Text($"Adresa: {partner.Adresa}, {partner.PttIMesto}");
                            }
                        });
                    });

                    col.Item().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(25);  // Rbr
                            columns.ConstantColumn(70);  // Šifra
                            columns.RelativeColumn(2);   // Naziv
                            columns.ConstantColumn(35);  // J.M.
                            columns.ConstantColumn(50);  // Kol
                            columns.ConstantColumn(60);  // Cena
                            columns.ConstantColumn(40);  // Rabat
                            columns.ConstantColumn(40);  // PDV%
                            columns.ConstantColumn(65);  // Bez PDV
                            columns.ConstantColumn(70);  // Ukupno
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("R.br").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Šifra").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Naziv artikla / usluge").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("J.M.").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Količina").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Cena").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Rab%").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("PDV%").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Bez PDV").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Ukupno").Bold().AlignRight();
                        });

                        foreach (var st in ponuda.Stavke.OrderBy(s => s.RedniBroj))
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(st.RedniBroj.ToString());
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(st.SifraArtikla).Bold();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(st.NazivArtikla);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(st.JedinicaMere);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.Kolicina:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.Cena:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.RabatProcenat:N0}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.PdvStopa:N0}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.IznosNeto:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.IznosBruto:N2}").AlignRight();
                        }
                    });

                    col.Item().PaddingTop(12).AlignRight().Width(260).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(100); });
                        t.Cell().Text("Ukupno osnovica bez PDV:").Bold();
                        t.Cell().Text($"{ponuda.UkupnoNeto:N2} RSD").AlignRight();
                        t.Cell().Text("Ukupno PDV:").Bold();
                        t.Cell().Text($"{ponuda.UkupnoPdv:N2} RSD").AlignRight();
                        t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text(jePredracun ? "ZA UPLATU:" : "UKUPNA VREDNOST PONUDE:").Bold().FontSize(11);
                        t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text($"{ponuda.UkupnoBruto:N2} RSD").Bold().FontSize(11).AlignRight();
                    });

                    if (!string.IsNullOrWhiteSpace(ponuda.Napomena))
                    {
                        col.Item().PaddingTop(15).Text($"Napomena: {ponuda.Napomena}").FontSize(8.5f);
                    }

                    col.Item().PaddingTop(20).Column(c =>
                    {
                        if (jePredracun)
                        {
                            c.Item().Text("Ovaj predračun ne predstavlja fakturu niti obavezu plaćanja, već poziv na uplatu po navedenim uslovima.").FontSize(8);
                            c.Item().Text("Predračun nije osnov za odbitak PDV-a.").FontSize(8);
                        }
                        else
                        {
                            c.Item().Text("Ova ponuda je informativnog karaktera i ne predstavlja obavezujući ugovor niti fakturu.").FontSize(8);
                            c.Item().Text($"Ponuda važi do {ponuda.RokVazenja:dd.MM.yyyy}, ukoliko drugačije nije naznačeno.").FontSize(8);
                        }
                        c.Item().Text("Ovaj dokument je punovažan bez potpisa i pečata.").FontSize(8);
                    });

                    col.Item().PaddingTop(25).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Sastavio:").Italic();
                            c.Item().PaddingTop(20).Text($"{ponuda.Korisnik}");
                        });
                        r.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("Kupac / Naručilac:").Italic();
                            c.Item().PaddingTop(20).Text("_______________________");
                        });
                    });
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiZapisnikONivelacijiPdf(NivelacijaCena niv, Firma firma)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(firma.Naziv).Bold().FontSize(13);
                        col.Item().Text($"PIB: {firma.Pib} | MB: {firma.MaticniBroj}");
                        col.Item().Text(firma.Adresa);
                    });
                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text("ZAPISNIK O NIVELACIJI CENA").Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
                        col.Item().Text($"Broj: {niv.BrojNivelacije}").Bold().FontSize(11);
                        col.Item().Text($"Datum: {niv.DatumNivelacije:dd.MM.yyyy}");
                        if (!string.IsNullOrWhiteSpace(niv.NazivMagacina))
                            col.Item().Text($"Magacin: {niv.NazivMagacina} ({niv.SifraMagacina})");
                    });
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(25);  // Rbr
                            columns.ConstantColumn(60);  // Šifra
                            columns.RelativeColumn(2);   // Naziv
                            columns.ConstantColumn(35);  // J.M.
                            columns.ConstantColumn(50);  // Kol
                            columns.ConstantColumn(55);  // Stara cena
                            columns.ConstantColumn(65);  // Iznos (staro)
                            columns.ConstantColumn(55);  // Nova cena
                            columns.ConstantColumn(65);  // Iznos (novo)
                            columns.ConstantColumn(65);  // Razlika
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("R.br").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Šifra").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Naziv artikla").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("J.M.").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Količina").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Stara cena").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Iznos").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Nova cena").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Iznos").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Razlika").Bold().AlignRight();
                        });

                        int rbr = 1;
                        decimal ukIznosStaro = 0m, ukIznosNovo = 0m;
                        foreach (var st in niv.Stavke)
                        {
                            decimal iznosStaro = st.KolicinaZaliha * st.StaraCena;
                            decimal iznosNovo = st.KolicinaZaliha * st.NovaCena;
                            ukIznosStaro += iznosStaro;
                            ukIznosNovo += iznosNovo;

                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(rbr.ToString());
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(st.SifraArtikla).Bold();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(st.NazivArtikla ?? string.Empty);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(st.JedinicaMere);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.KolicinaZaliha:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.StaraCena:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{iznosStaro:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.NovaCena:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{iznosNovo:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.UkupnaRazlika:N2}").AlignRight();
                            rbr++;
                        }

                        table.Cell().ColumnSpan(6).PaddingVertical(5).PaddingHorizontal(3).Text("TOTAL:").Bold().AlignRight();
                        table.Cell().PaddingVertical(5).PaddingHorizontal(3).Text($"{ukIznosStaro:N2}").Bold().AlignRight();
                        table.Cell().PaddingVertical(5).PaddingHorizontal(3).Text("");
                        table.Cell().PaddingVertical(5).PaddingHorizontal(3).Text($"{ukIznosNovo:N2}").Bold().AlignRight();
                        table.Cell().PaddingVertical(5).PaddingHorizontal(3).Text($"{niv.UkupnoRazlika:N2}").Bold().AlignRight();
                    });

                    col.Item().PaddingTop(12).AlignRight().Width(250).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(110); });
                        t.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("UKUPNA RAZLIKA:").Bold().FontSize(10);
                        t.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text($"{niv.UkupnoRazlika:N2} RSD").Bold().FontSize(10).AlignRight();
                    });

                    col.Item().PaddingTop(30).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Članovi komisije za nivelaciju:").Italic();
                            c.Item().PaddingTop(25).Text("1. _______________________");
                            c.Item().PaddingTop(10).Text("2. _______________________");
                        });
                        r.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("Odgovorno lice / Poslovođa:").Italic();
                            c.Item().PaddingTop(25).Text("_______________________");
                        });
                    });
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiRobniBrutoBilansPdf(Firma firma, List<RobniBrutoBilansRed> stavke, DateTime? doDatuma)
    {
        var poMagacinu = stavke
            .GroupBy(s => new { s.SifraMagacina, s.NazivMagacina })
            .OrderBy(g => g.Key.SifraMagacina)
            .ToList();

        decimal gDug = stavke.Sum(s => s.UlazVrednost);
        decimal gPot = stavke.Sum(s => s.IzlazVrednost);
        decimal gSal = stavke.Sum(s => s.SaldoVrednosni);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(15);
                page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Calibri"));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(firma.Naziv).Bold().FontSize(12);
                        col.Item().Text($"PIB: {firma.Pib} | MB: {firma.MaticniBroj}").FontSize(8.5f);
                        col.Item().Text(firma.Adresa).FontSize(8.5f);
                    });
                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text("BRUTO BILANS MATERIJALNOG KNJIGOVODSTVA").Bold().FontSize(12).FontColor(Colors.Blue.Darken2);
                        col.Item().Text($"Na dan: {(doDatuma.HasValue ? doDatuma.Value.ToString("dd.MM.yyyy") : DateTime.Now.ToString("dd.MM.yyyy"))}").Bold().FontSize(9.5f);
                    });
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    foreach (var grupa in poMagacinu)
                    {
                        col.Item().PaddingTop(6).PaddingBottom(2).Background(Colors.Grey.Lighten4).Padding(3).Row(r =>
                        {
                            r.RelativeItem().Text($"Računopolagač: {grupa.Key.SifraMagacina} — {grupa.Key.NazivMagacina}").Bold().FontSize(9f).FontColor(Colors.Blue.Darken3);
                        });

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(20);  // Rbr
                                columns.ConstantColumn(44);  // Šifra
                                columns.RelativeColumn(1);   // Naziv
                                columns.ConstantColumn(28);  // J.M.
                                columns.ConstantColumn(46);  // Cena
                                columns.ConstantColumn(42);  // Ulaz Kol
                                columns.ConstantColumn(55);  // Ulaz Vred (Dug)
                                columns.ConstantColumn(42);  // Izlaz Kol
                                columns.ConstantColumn(55);  // Izlaz Vred (Pot)
                                columns.ConstantColumn(44);  // Saldo Kol
                                columns.ConstantColumn(58);  // Saldo Vred
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("R.b").Bold().FontSize(7.5f);
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Šifra").Bold().FontSize(7.5f);
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Naziv artikla / robe").Bold().FontSize(7.5f);
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("J.M.").Bold().FontSize(7.5f);
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Cena").Bold().FontSize(7.5f).AlignRight();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Ulaz Kol").Bold().FontSize(7.5f).AlignRight();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Duguje (Ulaz)").Bold().FontSize(7.5f).AlignRight();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Izlaz Kol").Bold().FontSize(7.5f).AlignRight();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Potražuje").Bold().FontSize(7.5f).AlignRight();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Stanje Kol").Bold().FontSize(7.5f).AlignRight();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Saldo RSD").Bold().FontSize(7.5f).AlignRight();
                            });

                            int rbr = 1;
                            foreach (var st in grupa)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text(rbr.ToString()).FontSize(7.5f);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text(st.SifraArtikla).Bold().FontSize(7.5f);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text(st.NazivArtikla).FontSize(7.5f);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text(st.JedinicaMere).FontSize(7.5f);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{st.Cena:N2}").FontSize(7.5f).AlignRight();
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{st.UlazKolicina:N2}").FontSize(7.5f).AlignRight();
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{st.UlazVrednost:N2}").FontSize(7.5f).AlignRight();
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{st.IzlazKolicina:N2}").FontSize(7.5f).AlignRight();
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{st.IzlazVrednost:N2}").FontSize(7.5f).AlignRight();
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{st.SaldoKolicinski:N2}").FontSize(7.5f).AlignRight();
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{st.SaldoVrednosni:N2}").FontSize(7.5f).AlignRight();
                                rbr++;
                            }

                            decimal mDug = grupa.Sum(s => s.UlazVrednost);
                            decimal mPot = grupa.Sum(s => s.IzlazVrednost);
                            decimal mSal = grupa.Sum(s => s.SaldoVrednosni);

                            table.Cell().ColumnSpan(6).PaddingVertical(4).PaddingHorizontal(1.5f).Text($"UKUPNO ZA MAGACIN {grupa.Key.SifraMagacina}:").Bold().FontSize(7.5f).AlignRight();
                            table.Cell().PaddingVertical(4).PaddingHorizontal(1.5f).Text($"{mDug:N2}").Bold().FontSize(7.5f).AlignRight();
                            table.Cell().PaddingVertical(4).PaddingHorizontal(1.5f).Text("");
                            table.Cell().PaddingVertical(4).PaddingHorizontal(1.5f).Text($"{mPot:N2}").Bold().FontSize(7.5f).AlignRight();
                            table.Cell().PaddingVertical(4).PaddingHorizontal(1.5f).Text("");
                            table.Cell().PaddingVertical(4).PaddingHorizontal(1.5f).Text($"{mSal:N2}").Bold().FontSize(7.5f).AlignRight();
                        });
                    }

                    col.Item().PaddingTop(10).AlignRight().Width(350).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(120); });
                        t.Cell().Text("UKUPNO DUGUJE (SVI MAGACINI):").Bold().FontSize(8f);
                        t.Cell().Text($"{gDug:N2} RSD").FontSize(8f).AlignRight();
                        t.Cell().Text("UKUPNO POTRAŽUJE (SVI MAGACINI):").Bold().FontSize(8f);
                        t.Cell().Text($"{gPot:N2} RSD").FontSize(8f).AlignRight();
                        t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("UKUPAN SALDO ZALIHA:").Bold().FontSize(9f);
                        t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text($"{gSal:N2} RSD").Bold().FontSize(9f).AlignRight();
                    });
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    /// <summary>Vrednovanje zaliha — trenutno stanje (količina, jedinična prosečna cena, vrednost) po magacinima, samo artikli sa stanjem različitim od nule.</summary>
    public static byte[] GenerisiVrednovanjeZalihaPdf(Firma firma, List<RobniBrutoBilansRed> stavke)
    {
        var poMagacinu = stavke
            .Where(s => s.SaldoKolicinski != 0)
            .GroupBy(s => new { s.SifraMagacina, s.NazivMagacina })
            .OrderBy(g => g.Key.SifraMagacina)
            .ToList();

        decimal gVrednost = poMagacinu.Sum(g => g.Sum(s => s.SaldoVrednosni));

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(15);
                page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Calibri"));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(firma.Naziv).Bold().FontSize(12);
                        col.Item().Text($"PIB: {firma.Pib} | MB: {firma.MaticniBroj}").FontSize(8.5f);
                        col.Item().Text(firma.Adresa).FontSize(8.5f);
                    });
                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text("VREDNOVANJE ZALIHA").Bold().FontSize(13).FontColor(Colors.Blue.Darken2);
                        col.Item().Text($"Na dan: {DateTime.Now:dd.MM.yyyy}").Bold().FontSize(9.5f);
                    });
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    foreach (var grupa in poMagacinu)
                    {
                        col.Item().PaddingTop(6).PaddingBottom(2).Background(Colors.Grey.Lighten4).Padding(3).Row(r =>
                        {
                            r.RelativeItem().Text($"Magacin: {grupa.Key.SifraMagacina} — {grupa.Key.NazivMagacina}").Bold().FontSize(9f).FontColor(Colors.Blue.Darken3);
                        });

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(20);  // Rbr
                                columns.ConstantColumn(48);  // Šifra
                                columns.RelativeColumn(1);   // Naziv
                                columns.ConstantColumn(30);  // J.M.
                                columns.ConstantColumn(55);  // Količina
                                columns.ConstantColumn(60);  // Jedinična cena
                                columns.ConstantColumn(65);  // Vrednost
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("R.b").Bold().FontSize(7.5f);
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Šifra").Bold().FontSize(7.5f);
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Naziv artikla / robe").Bold().FontSize(7.5f);
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("J.M.").Bold().FontSize(7.5f);
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Količina").Bold().FontSize(7.5f).AlignRight();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Jed. cena").Bold().FontSize(7.5f).AlignRight();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Vrednost").Bold().FontSize(7.5f).AlignRight();
                            });

                            int rbr = 1;
                            foreach (var st in grupa.OrderBy(s => s.SifraArtikla))
                            {
                                decimal jedCena = st.SaldoKolicinski != 0 ? st.SaldoVrednosni / st.SaldoKolicinski : 0m;

                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text(rbr.ToString()).FontSize(7.5f);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text(st.SifraArtikla).Bold().FontSize(7.5f);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text(st.NazivArtikla).FontSize(7.5f);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text(st.JedinicaMere).FontSize(7.5f);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{st.SaldoKolicinski:N2}").FontSize(7.5f).AlignRight();
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{jedCena:N2}").FontSize(7.5f).AlignRight();
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{st.SaldoVrednosni:N2}").FontSize(7.5f).AlignRight();
                                rbr++;
                            }

                            decimal mVrednost = grupa.Sum(s => s.SaldoVrednosni);

                            table.Cell().ColumnSpan(6).PaddingVertical(4).PaddingHorizontal(1.5f).Text($"UKUPNO ZA MAGACIN {grupa.Key.SifraMagacina}:").Bold().FontSize(7.5f).AlignRight();
                            table.Cell().PaddingVertical(4).PaddingHorizontal(1.5f).Text($"{mVrednost:N2}").Bold().FontSize(7.5f).AlignRight();
                        });
                    }

                    col.Item().PaddingTop(10).AlignRight().Width(300).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(120); });
                        t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("UKUPNA VREDNOST ZALIHA (SVI MAGACINI):").Bold().FontSize(9f);
                        t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text($"{gVrednost:N2} RSD").Bold().FontSize(9f).AlignRight();
                    });
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    /// <summary>Raspored artikala - analitika (MAT1.PRG:mat91) — za svaki artikal, jedna sekcija sa stanjem/cenom/vrednošću po magacinu na zadati datum, sa TOTAL redom.</summary>
    public static byte[] GenerisiRasporedArtikalaPdf(Firma firma, List<RobniBrutoBilansRed> stavke, DateTime? doDatuma)
    {
        var poArtiklu = stavke
            .GroupBy(s => new { s.SifraArtikla, s.NazivArtikla, s.JedinicaMere })
            .OrderBy(g => g.Key.SifraArtikla)
            .ToList();

        return Document.Create(container =>
        {
            foreach (var grupa in poArtiklu)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(firma.Naziv).Bold().FontSize(13);
                            col.Item().Text($"PIB: {firma.Pib} | MB: {firma.MaticniBroj}");
                            col.Item().Text(firma.Adresa);
                        });
                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text("RASPORED ARTIKALA - ANALITIKA").Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
                            col.Item().Text($"Do datuma: {(doDatuma.HasValue ? doDatuma.Value.ToString("dd.MM.yyyy") : DateTime.Now.ToString("dd.MM.yyyy"))}").FontSize(10);
                            col.Item().Text($"Artikal: {grupa.Key.NazivArtikla} ({grupa.Key.SifraArtikla})").Bold().FontSize(10);
                            col.Item().Text($"J.M.: {grupa.Key.JedinicaMere}");
                        });
                    });

                    page.Content().PaddingVertical(15).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(60);  // Šifra magacina
                                columns.RelativeColumn(2);   // Naziv magacina
                                columns.ConstantColumn(80);  // Stanje
                                columns.ConstantColumn(90);  // Cena
                                columns.ConstantColumn(100); // Vrednost
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Šifra").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Magacin / Računopolagač").Bold();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Stanje").Bold().AlignRight();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Cena").Bold().AlignRight();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Vrednost").Bold().AlignRight();
                            });

                            decimal tot1 = 0m, tot2 = 0m;
                            foreach (var red in grupa.OrderBy(r => r.SifraMagacina))
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(red.SifraMagacina);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(red.NazivMagacina);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{red.SaldoKolicinski:N2}").AlignRight();
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{red.Cena:N2}").AlignRight();
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{red.SaldoVrednosni:N2}").AlignRight();
                                tot1 += red.SaldoKolicinski;
                                tot2 += red.SaldoVrednosni;
                            }

                            table.Cell().ColumnSpan(2).PaddingVertical(5).PaddingHorizontal(3).Text("TOTAL:").Bold().AlignRight();
                            table.Cell().PaddingVertical(5).PaddingHorizontal(3).Text($"{tot1:N2}").Bold().AlignRight();
                            table.Cell().PaddingVertical(5).PaddingHorizontal(3).Text("");
                            table.Cell().PaddingVertical(5).PaddingHorizontal(3).Text($"{tot2:N2}").Bold().AlignRight();
                        });
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("Stranica ");
                        x.CurrentPageNumber();
                        x.Span(" od ");
                        x.TotalPages();
                    });
                });
            }
        }).GeneratePdf();
    }

    /// <summary>Stanje po artiklima - sintetika (MAT1.PRG:mat92) — jedan red po artiklu, sumirano preko svih magacina, do zadatog datuma. Artikli čiji su i stanje i saldo tačno 0 se izostavljaju (isto kao legacy), ali TOTAL red sabira baš svaku stavku, uključujući izostavljene.</summary>
    public static byte[] GenerisiStanjePoArtiklimaPdf(Firma firma, List<RobniBrutoBilansRed> stavke, DateTime? doDatuma)
    {
        var poArtiklu = stavke
            .GroupBy(s => new { s.SifraArtikla, s.NazivArtikla, s.Pakovanje, s.JedinicaMere })
            .Select(g => new
            {
                g.Key.SifraArtikla,
                g.Key.NazivArtikla,
                g.Key.Pakovanje,
                g.Key.JedinicaMere,
                Ulaz = g.Sum(r => r.UlazKolicina),
                Izlaz = g.Sum(r => r.IzlazKolicina),
                Duguje = g.Sum(r => r.UlazVrednost),
                Potrazuje = g.Sum(r => r.IzlazVrednost)
            })
            .OrderBy(a => a.SifraArtikla)
            .ToList();

        decimal tUlaz = poArtiklu.Sum(a => a.Ulaz);
        decimal tIzlaz = poArtiklu.Sum(a => a.Izlaz);
        decimal tDuguje = poArtiklu.Sum(a => a.Duguje);
        decimal tPotrazuje = poArtiklu.Sum(a => a.Potrazuje);

        var zaPrikaz = poArtiklu.Where(a => (a.Ulaz - a.Izlaz) != 0 || (a.Duguje - a.Potrazuje) != 0).ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(15);
                page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Calibri"));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(firma.Naziv).Bold().FontSize(12);
                        col.Item().Text($"PIB: {firma.Pib} | MB: {firma.MaticniBroj}").FontSize(8.5f);
                        col.Item().Text(firma.Adresa).FontSize(8.5f);
                    });
                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text("STANJE PO ARTIKLIMA").Bold().FontSize(13).FontColor(Colors.Blue.Darken2);
                        col.Item().Text($"sa datumom: {(doDatuma.HasValue ? doDatuma.Value.ToString("dd.MM.yyyy") : DateTime.Now.ToString("dd.MM.yyyy"))}").Bold().FontSize(9.5f);
                    });
                });

                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(48);  // Šifra
                        columns.RelativeColumn(1);   // Naziv
                        columns.ConstantColumn(48);  // Pakovanje
                        columns.ConstantColumn(30);  // J.M.
                        columns.ConstantColumn(45);  // Ulaz
                        columns.ConstantColumn(45);  // Izlaz
                        columns.ConstantColumn(48);  // Stanje
                        columns.ConstantColumn(52);  // Duguje
                        columns.ConstantColumn(52);  // Potražuje
                        columns.ConstantColumn(56);  // Saldo
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Šifra").Bold().FontSize(7.5f);
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Artikal").Bold().FontSize(7.5f);
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Pakovanje").Bold().FontSize(7.5f);
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("J.M.").Bold().FontSize(7.5f);
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Ulaz").Bold().FontSize(7.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Izlaz").Bold().FontSize(7.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Stanje").Bold().FontSize(7.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Duguje").Bold().FontSize(7.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Potražuje").Bold().FontSize(7.5f).AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Saldo").Bold().FontSize(7.5f).AlignRight();
                    });

                    foreach (var a in zaPrikaz)
                    {
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text(a.SifraArtikla).FontSize(7.5f);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text(a.NazivArtikla).FontSize(7.5f);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text(a.Pakovanje ?? "").FontSize(7.5f);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text(a.JedinicaMere).FontSize(7.5f);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{a.Ulaz:N2}").FontSize(7.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{a.Izlaz:N2}").FontSize(7.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{(a.Ulaz - a.Izlaz):N2}").FontSize(7.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{a.Duguje:N2}").FontSize(7.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{a.Potrazuje:N2}").FontSize(7.5f).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{(a.Duguje - a.Potrazuje):N2}").FontSize(7.5f).AlignRight();
                    }

                    table.Cell().ColumnSpan(4).PaddingVertical(4).PaddingHorizontal(1.5f).Text("T O T A L :").Bold().FontSize(7.5f).AlignRight();
                    table.Cell().PaddingVertical(4).PaddingHorizontal(1.5f).Text($"{tUlaz:N2}").Bold().FontSize(7.5f).AlignRight();
                    table.Cell().PaddingVertical(4).PaddingHorizontal(1.5f).Text($"{tIzlaz:N2}").Bold().FontSize(7.5f).AlignRight();
                    table.Cell().PaddingVertical(4).PaddingHorizontal(1.5f).Text($"{(tUlaz - tIzlaz):N2}").Bold().FontSize(7.5f).AlignRight();
                    table.Cell().PaddingVertical(4).PaddingHorizontal(1.5f).Text($"{tDuguje:N2}").Bold().FontSize(7.5f).AlignRight();
                    table.Cell().PaddingVertical(4).PaddingHorizontal(1.5f).Text($"{tPotrazuje:N2}").Bold().FontSize(7.5f).AlignRight();
                    table.Cell().PaddingVertical(4).PaddingHorizontal(1.5f).Text($"{(tDuguje - tPotrazuje):N2}").Bold().FontSize(7.5f).AlignRight();
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiRobnuKarticuPdf(Firma firma, Magacin magacin, Artikal artikal, List<MaterijalnaKartica> kartice)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(15);
                page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Calibri"));

                ComponujMagacinskuKarticu(page, firma, magacin, artikal.SifraArtikla, artikal.Naziv, artikal.JedinicaMere, artikal.ProdajnaCena, kartice, "ROBNA KARTICA GLAVNE KNJIGE");
            });
        }).GeneratePdf();
    }

    /// <summary>Generiše PDF sa robnim karticama za sve (magacin, artikal) parove sa prometom — jedna sekcija po paru. Poziva se i za jedan magacin i za sve magacine odjednom.</summary>
    public static byte[] GenerisiSveRobneKarticePdf(Firma firma, List<(Magacin Magacin, Artikal Artikal, List<MaterijalnaKartica> Kartice)> sveKartice)
    {
        return Document.Create(container =>
        {
            foreach (var (magacin, artikal, kartice) in sveKartice)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(15);
                    page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Calibri"));

                    ComponujMagacinskuKarticu(page, firma, magacin, artikal.SifraArtikla, artikal.Naziv, artikal.JedinicaMere, artikal.ProdajnaCena, kartice, "ROBNA KARTICA GLAVNE KNJIGE");
                });
            }
        }).GeneratePdf();
    }

    public static byte[] GenerisiMaterijalnuKarticuPdf(Firma firma, Magacin magacin, Materijal materijal, List<MaterijalnaKartica> kartice)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(15);
                page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Calibri"));

                ComponujMagacinskuKarticu(page, firma, magacin, materijal.SifraArtikla, materijal.Naziv, materijal.JedinicaMere, null, kartice, "MATERIJALNA KARTICA GLAVNE KNJIGE");
            });
        }).GeneratePdf();
    }

    /// <summary>Generiše PDF sa materijalnim karticama za sve (magacin, materijal) parove sa prometom — jedna sekcija po paru.</summary>
    public static byte[] GenerisiSveMaterijalneKarticePdf(Firma firma, List<(Magacin Magacin, Materijal Materijal, List<MaterijalnaKartica> Kartice)> sveKartice)
    {
        return Document.Create(container =>
        {
            foreach (var (magacin, materijal, kartice) in sveKartice)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(15);
                    page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Calibri"));

                    ComponujMagacinskuKarticu(page, firma, magacin, materijal.SifraArtikla, materijal.Naziv, materijal.JedinicaMere, null, kartice, "MATERIJALNA KARTICA GLAVNE KNJIGE");
                });
            }
        }).GeneratePdf();
    }

    private static void ComponujMagacinskuKarticu(PageDescriptor page, Firma firma, Magacin magacin, string sifraArtikla, string nazivArtikla, string jedinicaMere, decimal? prodajnaCena, List<MaterijalnaKartica> kartice, string naslov)
    {
        page.Header().Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(firma.Naziv).Bold().FontSize(12);
                col.Item().Text($"PIB: {firma.Pib} | MB: {firma.MaticniBroj}").FontSize(8.5f);
                col.Item().Text(firma.Adresa).FontSize(8.5f);
            });
            row.RelativeItem().AlignRight().Column(col =>
            {
                col.Item().Text(naslov).Bold().FontSize(13).FontColor(Colors.Blue.Darken2);
                col.Item().Text($"Računopolagač: {magacin.NazivMagacina} ({magacin.SifraMagacina})").Bold().FontSize(9.5f);
                col.Item().Text($"Artikal: {nazivArtikla} ({sifraArtikla})").Bold().FontSize(9.5f);
                col.Item().Text(prodajnaCena.HasValue ? $"J.M.: {jedinicaMere} | Prod. cena: {prodajnaCena:N2} RSD" : $"J.M.: {jedinicaMere}").FontSize(8.5f);
            });
        });

        page.Content().PaddingVertical(10).Column(col =>
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(22);  // Rbr
                    columns.ConstantColumn(52);  // Datum
                    columns.RelativeColumn(1);   // Opis
                    columns.ConstantColumn(42);  // Ulaz
                    columns.ConstantColumn(42);  // Izlaz
                    columns.ConstantColumn(46);  // Stanje
                    columns.ConstantColumn(46);  // Cena
                    columns.ConstantColumn(54);  // Duguje
                    columns.ConstantColumn(54);  // Potražuje
                    columns.ConstantColumn(58);  // Saldo
                    columns.ConstantColumn(48);  // Pros. cena
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("R.br").Bold().FontSize(7.5f);
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Datum").Bold().FontSize(7.5f);
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Opis promene").Bold().FontSize(7.5f);
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Ulaz").Bold().FontSize(7.5f).AlignRight();
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Izlaz").Bold().FontSize(7.5f).AlignRight();
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Stanje").Bold().FontSize(7.5f).AlignRight();
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Cena").Bold().FontSize(7.5f).AlignRight();
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Duguje").Bold().FontSize(7.5f).AlignRight();
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Potražuje").Bold().FontSize(7.5f).AlignRight();
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Saldo").Bold().FontSize(7.5f).AlignRight();
                    header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(1.5f).Text("Pros. cena").Bold().FontSize(7.5f).AlignRight();
                });

                int rbr = 1;
                foreach (var st in kartice)
                {
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text(rbr.ToString()).FontSize(7.5f);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{st.DatumPromene:dd.MM.yyyy}").FontSize(7.5f);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text(st.OpisPromene ?? "").FontSize(7.5f);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{st.Ulaz:N2}").FontSize(7.5f).AlignRight();
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{st.Izlaz:N2}").FontSize(7.5f).AlignRight();
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{st.Stanje:N2}").FontSize(7.5f).AlignRight();
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{st.Cena:N2}").FontSize(7.5f).AlignRight();
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{st.Duguje:N2}").FontSize(7.5f).AlignRight();
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{st.Potrazuje:N2}").FontSize(7.5f).AlignRight();
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{st.Saldo:N2}").FontSize(7.5f).AlignRight();
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(1.5f).Text($"{st.CenaIzlaz:N2}").FontSize(7.5f).AlignRight();
                    rbr++;
                }

                decimal ukUlazT = kartice.Sum(k => k.Ulaz);
                decimal ukIzlazT = kartice.Sum(k => k.Izlaz);
                decimal ukDugujeT = kartice.Sum(k => k.Duguje);
                decimal ukPotrazujeT = kartice.Sum(k => k.Potrazuje);
                decimal zadnjeStanjeT = kartice.LastOrDefault()?.Stanje ?? 0m;
                decimal zadnjiSaldoT = kartice.LastOrDefault()?.Saldo ?? 0m;
                decimal zadnjaCenaT = kartice.LastOrDefault()?.Cena ?? 0m;
                decimal zadnjaProsCenaT = kartice.LastOrDefault()?.CenaIzlaz ?? 0m;

                table.Cell().ColumnSpan(3).PaddingVertical(4).PaddingHorizontal(1.5f).Text("TOTAL:").Bold().FontSize(7.5f).AlignRight();
                table.Cell().PaddingVertical(4).PaddingHorizontal(1.5f).Text($"{ukUlazT:N2}").Bold().FontSize(7.5f).AlignRight();
                table.Cell().PaddingVertical(4).PaddingHorizontal(1.5f).Text($"{ukIzlazT:N2}").Bold().FontSize(7.5f).AlignRight();
                table.Cell().PaddingVertical(4).PaddingHorizontal(1.5f).Text($"{zadnjeStanjeT:N2}").Bold().FontSize(7.5f).AlignRight();
                table.Cell().PaddingVertical(4).PaddingHorizontal(1.5f).Text($"{zadnjaCenaT:N2}").Bold().FontSize(7.5f).AlignRight();
                table.Cell().PaddingVertical(4).PaddingHorizontal(1.5f).Text($"{ukDugujeT:N2}").Bold().FontSize(7.5f).AlignRight();
                table.Cell().PaddingVertical(4).PaddingHorizontal(1.5f).Text($"{ukPotrazujeT:N2}").Bold().FontSize(7.5f).AlignRight();
                table.Cell().PaddingVertical(4).PaddingHorizontal(1.5f).Text($"{zadnjiSaldoT:N2}").Bold().FontSize(7.5f).AlignRight();
                table.Cell().PaddingVertical(4).PaddingHorizontal(1.5f).Text($"{zadnjaProsCenaT:N2}").Bold().FontSize(7.5f).AlignRight();
            });
        });

        page.Footer().AlignRight().Text(x =>
        {
            x.Span("Stranica ");
            x.CurrentPageNumber();
            x.Span(" od ");
            x.TotalPages();
        });
    }

    /// <summary>Provera materijalnih kartica — spisak redova sa negativnim stanjem ili negativnom cenom (legacy provera_m_kart()/provera_naslov()).</summary>
    public static byte[] GenerisiProveruMaterijalnihKarticaPdf(Firma firma, List<MaterijalnaKartica> redovi)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(firma.Naziv).Bold().FontSize(13);
                        col.Item().Text($"PIB: {firma.Pib} | MB: {firma.MaticniBroj}");
                    });
                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text("PROVERA MATERIJALNIH KARTICA").Bold().FontSize(14).FontColor(Colors.Red.Darken2);
                        col.Item().Text("Negativna stanja i negativne cene");
                    });
                });

                page.Content().PaddingVertical(15).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(60);  // Magacin
                        columns.ConstantColumn(70);  // Artikal
                        columns.ConstantColumn(50);  // R.br
                        columns.ConstantColumn(70);  // Datum
                        columns.RelativeColumn(2);   // Opis promene
                        columns.ConstantColumn(80);  // Cena
                        columns.ConstantColumn(80);  // Stanje
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Magacin").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Artikal").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("R.br").Bold().AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Datum").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Opis promene").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Cena").Bold().AlignRight();
                        header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Stanje").Bold().AlignRight();
                    });

                    foreach (var r in redovi)
                    {
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(r.SifraMagacina);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(r.SifraArtikla);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(r.RedniBroj.ToString()).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{r.DatumPromene:dd.MM.yyyy}");
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(r.OpisPromene ?? "");
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{r.Cena:N2}").FontColor(r.Cena < 0 ? Colors.Red.Darken2 : Colors.Black).AlignRight();
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{r.Stanje:N2}").FontColor(r.Stanje < 0 ? Colors.Red.Darken2 : Colors.Black).AlignRight();
                    }
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    /// <summary>Štampa jednog Ulaza materijala (legacy stampa_ulaza() — M2.PRG), sa stavkama.</summary>
    public static byte[] GenerisiUlazPdf(Firma firma, UlazNalog nalog, Dictionary<string, Materijal> artikliMap, Magacin magacin)
    {
        return GenerisiMagacinskiNalogPdf(firma, "ULAZ MATERIJALA", nalog.BrojNaloga.ToString(), nalog.Datum, magacin, null,
            nalog.Stavke.Select(s => (s.SifraArtikla, artikliMap.TryGetValue(s.SifraArtikla, out var a) ? a.Naziv : s.SifraArtikla, artikliMap.TryGetValue(s.SifraArtikla, out var a2) ? a2.JedinicaMere : "", s.Kolicina, (decimal?)s.Cena, s.Iznos)).ToList(),
            $"Broj računa: {nalog.BrojRacuna}");
    }

    /// <summary>Štampa jednog Trebovanja (legacy stampa_treb() — M3.PRG), sa stavkama.</summary>
    public static byte[] GenerisiTrebovanjePdf(Firma firma, TrebovanjeNalog nalog, Dictionary<string, Materijal> artikliMap, Magacin magacin)
    {
        return GenerisiMagacinskiNalogPdf(firma, "TREBOVANJE", nalog.BrojNaloga.ToString(), nalog.Datum, magacin, null,
            nalog.Stavke.Select(s => (s.SifraArtikla, artikliMap.TryGetValue(s.SifraArtikla, out var a) ? a.Naziv : s.SifraArtikla, artikliMap.TryGetValue(s.SifraArtikla, out var a2) ? a2.JedinicaMere : "", s.Kolicina, (decimal?)s.Cena, s.Iznos)).ToList(),
            null);
    }

    private static byte[] GenerisiMagacinskiNalogPdf(Firma firma, string naslov, string brojNaloga, DateTime datum,
        Magacin magacin, Magacin? magacinPrima,
        List<(string SifraArtikla, string NazivArtikla, string JedinicaMere, decimal Kolicina, decimal? Cena, decimal Iznos)> stavke,
        string? dodatnaLinija)
    {
        bool imaCenu = stavke.Any(s => s.Cena.HasValue);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(firma.Naziv).Bold().FontSize(13);
                        col.Item().Text($"PIB: {firma.Pib} | MB: {firma.MaticniBroj}");
                    });
                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text($"{naslov} br. {brojNaloga}").Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
                        col.Item().Text($"Datum: {datum:dd.MM.yyyy}");
                        if (magacinPrima != null)
                        {
                            col.Item().Text($"Magacin daje: {magacin.SifraMagacina} - {magacin.NazivMagacina} | Magacin prima: {magacinPrima.SifraMagacina} - {magacinPrima.NazivMagacina}");
                        }
                        else
                        {
                            col.Item().Text($"Magacin: {magacin.SifraMagacina} - {magacin.NazivMagacina}");
                        }
                        if (!string.IsNullOrWhiteSpace(dodatnaLinija)) col.Item().Text(dodatnaLinija);
                    });
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(70);   // Šifra
                            columns.RelativeColumn(3);    // Naziv
                            columns.ConstantColumn(50);   // J.M.
                            columns.ConstantColumn(80);   // Količina
                            if (imaCenu)
                            {
                                columns.ConstantColumn(80);   // Cena
                                columns.ConstantColumn(90);   // Iznos
                            }
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Šifra").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Naziv artikla").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("J.M.").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Količina").Bold().AlignRight();
                            if (imaCenu)
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Cena").Bold().AlignRight();
                                header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Iznos").Bold().AlignRight();
                            }
                        });

                        decimal ukIznos = 0;
                        foreach (var s in stavke)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(s.SifraArtikla);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(s.NazivArtikla);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(s.JedinicaMere);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{s.Kolicina:N2}").AlignRight();
                            if (imaCenu)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(s.Cena.HasValue ? $"{s.Cena:N2}" : "").AlignRight();
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{s.Iznos:N2}").AlignRight();
                            }
                            ukIznos += s.Iznos;
                        }

                        if (imaCenu)
                        {
                            table.Cell().ColumnSpan(4).PaddingVertical(5).PaddingHorizontal(3).Text("TOTAL:").Bold().AlignRight();
                            table.Cell().PaddingVertical(5).PaddingHorizontal(3).Text("");
                            table.Cell().PaddingVertical(5).PaddingHorizontal(3).Text($"{ukIznos:N2}").Bold().AlignRight();
                        }
                    });
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Stranica ");
                    x.CurrentPageNumber();
                    x.Span(" od ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }
}
