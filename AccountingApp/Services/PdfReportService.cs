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
                            columns.ConstantColumn(70);
                            columns.ConstantColumn(75);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(90);
                            columns.ConstantColumn(90);
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
            });
        }).GeneratePdf();
    }

    public static byte[] GenerisiIOSPdf(Firma firma, Partner partner, List<KarticaRed> stavke)
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
                    col.Item().PaddingTop(10).Text("IZVOD OTVORENIH STAVKI (IOS)").Bold().FontSize(16).AlignCenter();
                    col.Item().Text($"{partner.SifraPartnera} — {partner.Naziv}").FontSize(12).AlignCenter();
                    if (!string.IsNullOrWhiteSpace(partner.Adresa))
                    {
                        col.Item().Text($"{partner.Adresa}, {partner.PttIMesto}").FontSize(9).FontColor(Colors.Grey.Medium).AlignCenter();
                    }
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(75);
                            columns.ConstantColumn(75);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(90);
                            columns.ConstantColumn(90);
                            columns.ConstantColumn(90);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Datum").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Nalog").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).Text("Opis").Bold();
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
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text($"{s.Duguje:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text($"{s.Potrazuje:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4).Text($"{s.Saldo:N2}").AlignRight();
                        }

                        table.Cell().ColumnSpan(3).PaddingVertical(3).PaddingHorizontal(4).Text("UKUPNO:").Bold().AlignRight();
                        table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text($"{zbirDuguje:N2}").Bold().AlignRight();
                        table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text($"{zbirPotrazuje:N2}").Bold().AlignRight();
                        table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text($"{(stavke.Count > 0 ? stavke[^1].Saldo : 0m):N2}").Bold().AlignRight();
                    });

                    if (stavke.Count == 0)
                    {
                        col.Item().PaddingTop(20).AlignCenter().Text("Nema proknjiženih stavki vezanih za ovog partnera.").FontColor(Colors.Grey.Medium).Italic();
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
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(s.BrojNaloga);
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
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

                page.Header().Column(col =>
                {
                    col.Item().Text(firma.Naziv).Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                    col.Item().Text($"{firma.Adresa}, {firma.PttIMesto} | PIB: {firma.Pib ?? "---"} | Žiro: {firma.ZiroRacun ?? "---"}").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(10).Text(naslov).Bold().FontSize(16).AlignCenter();
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
                            columns.ConstantColumn(80);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(100);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Konto").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Naziv konta").Bold();
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

                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(r.BrojKonta);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(r.NazivKonta);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{r.Duguje:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{r.Potrazuje:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{r.Saldo:N2}").AlignRight();
                        }

                        table.Cell().ColumnSpan(2).Padding(6).Text("UKUPNO:").Bold().AlignRight();
                        table.Cell().Padding(6).Text($"{zbirDuguje:N2}").Bold().AlignRight();
                        table.Cell().Padding(6).Text($"{zbirPotrazuje:N2}").Bold().AlignRight();
                        table.Cell().Padding(6).Text($"{zbirSaldo:N2}").Bold().AlignRight();
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
}
