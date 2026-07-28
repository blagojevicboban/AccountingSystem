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
                            columns.ConstantColumn(60);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(85);
                            columns.ConstantColumn(85);
                            columns.ConstantColumn(85);
                            columns.ConstantColumn(85);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Konto").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Naziv konta").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Duguje").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Potražuje").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Saldo duguje").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Saldo potražuje").Bold().AlignRight();
                        });

                        decimal zbirDuguje = 0, zbirPotrazuje = 0, zbirSaldoDuguje = 0, zbirSaldoPotrazuje = 0;

                        foreach (var r in redovi)
                        {
                            if (r.Tip != BrutoBilansRedTip.Detalj)
                            {
                                var pozadina = r.Tip == BrutoBilansRedTip.KlasaTotal ? Colors.Grey.Lighten2 : Colors.Grey.Lighten4;
                                table.Cell().ColumnSpan(2).Background(pozadina).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(r.NazivKonta).Bold();
                                table.Cell().Background(pozadina).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{r.Duguje:N2}").Bold().AlignRight();
                                table.Cell().Background(pozadina).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{r.Potrazuje:N2}").Bold().AlignRight();
                                table.Cell().Background(pozadina).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{r.SaldoDuguje:N2}").Bold().AlignRight();
                                table.Cell().Background(pozadina).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{r.SaldoPotrazuje:N2}").Bold().AlignRight();
                                continue;
                            }

                            zbirDuguje += r.Duguje;
                            zbirPotrazuje += r.Potrazuje;
                            zbirSaldoDuguje += r.SaldoDuguje;
                            zbirSaldoPotrazuje += r.SaldoPotrazuje;

                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(r.BrojKonta);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(r.NazivKonta);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{r.Duguje:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{r.Potrazuje:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{r.SaldoDuguje:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{r.SaldoPotrazuje:N2}").AlignRight();
                        }

                        table.Cell().ColumnSpan(2).Padding(6).Text("UKUPNO:").Bold().AlignRight();
                        table.Cell().Padding(6).Text($"{zbirDuguje:N2}").Bold().AlignRight();
                        table.Cell().Padding(6).Text($"{zbirPotrazuje:N2}").Bold().AlignRight();
                        table.Cell().Padding(6).Text($"{zbirSaldoDuguje:N2}").Bold().AlignRight();
                        table.Cell().Padding(6).Text($"{zbirSaldoPotrazuje:N2}").Bold().AlignRight();
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
                    col.Item().PaddingTop(10).Text($"NALOG ZA PRIMOPREDAJU ROBE Br. {nalog.BrojNaloga}").Bold().FontSize(16).AlignCenter();
                    col.Item().Text($"Datum: {nalog.Datum:dd.MM.yyyy}").FontSize(10).AlignCenter();
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(10).Column(col =>
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

                    col.Item().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(35);
                            columns.ConstantColumn(75);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(75);
                            columns.ConstantColumn(85);
                            columns.ConstantColumn(95);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("R.br").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Šifra").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).Text("Naziv artikla").Bold();
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
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text($"{st.Kolicina:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text($"{st.Cena:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4).Text($"{st.Iznos:N2}").AlignRight();
                            ukupnoVrednost += st.Iznos;
                            rbr++;
                        }

                        table.Cell().ColumnSpan(5).PaddingVertical(6).PaddingHorizontal(4).Text("UKUPNA VREDNOST PRIMOPREDAJE:").Bold().AlignRight();
                        table.Cell().PaddingVertical(6).PaddingHorizontal(4).Text($"{ukupnoVrednost:N2} RSD").Bold().AlignRight();
                    });

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
                            col.Item().Text($"RAČUN - OTPREMNICA br. {racun.BrojRacuna}").Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
                            col.Item().Text($"Mesto i datum izdavanja: {firma.PttIMesto ?? "Beograd"}, {racun.DatumRacuna:dd.MM.yyyy}.");
                            col.Item().Text($"Datum prometa: {racun.DatumOtpremnice:dd.MM.yyyy}.");
                            col.Item().Text($"Rok plaćanja: {racun.DatumRacuna.AddDays(racun.RokPlacanjaDana):dd.MM.yyyy}. ({racun.RokPlacanjaDana} dana)");
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
                            columns.ConstantColumn(30);  // Rbr
                            columns.ConstantColumn(80);  // Šifra
                            columns.RelativeColumn(2);   // Naziv
                            columns.ConstantColumn(55);  // Kol
                            columns.ConstantColumn(65);  // Cena
                            columns.ConstantColumn(50);  // Rabat
                            columns.ConstantColumn(45);  // PDV%
                            columns.ConstantColumn(70);  // Bez PDV
                            columns.ConstantColumn(75);  // Ukupno
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("R.br").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Šifra").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Naziv artikla / robe").Bold();
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
                            columns.ConstantColumn(30);  // Rbr
                            columns.ConstantColumn(75);  // Šifra
                            columns.RelativeColumn(2);   // Naziv
                            columns.ConstantColumn(50);  // J.M.
                            columns.ConstantColumn(55);  // Kol
                            columns.ConstantColumn(65);  // Stara cena
                            columns.ConstantColumn(65);  // Nova cena
                            columns.ConstantColumn(60);  // Razl/jed
                            columns.ConstantColumn(75);  // Uk. razlika
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("R.br").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Šifra").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Artikal / Roba").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("J.M.").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Zaliha").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Stara C.").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Nova C.").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Razlika").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Uk. Razlika").Bold().AlignRight();
                        });

                        int rbr = 1;
                        foreach (var st in niv.Stavke)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(rbr.ToString());
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(st.SifraArtikla).Bold();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(st.NazivArtikla ?? string.Empty);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(st.JedinicaMere);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.KolicinaZaliha:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.StaraCena:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.NovaCena:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.RazlikaPoJedinici:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.UkupnaRazlika:N2}").AlignRight();
                            rbr++;
                        }
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
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Calibri"));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(firma.Naziv).Bold().FontSize(12);
                        col.Item().Text($"PIB: {firma.Pib} | MB: {firma.MaticniBroj}");
                        col.Item().Text(firma.Adresa);
                    });
                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text("ROBNI / MATERIJALNI BRUTO BILANS").Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
                        col.Item().Text($"Na dan: {(doDatuma.HasValue ? doDatuma.Value.ToString("dd.MM.yyyy") : DateTime.Now.ToString("dd.MM.yyyy"))}").Bold().FontSize(10);
                    });
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(25);  // Rbr
                            columns.ConstantColumn(65);  // Magacin
                            columns.ConstantColumn(75);  // Šifra
                            columns.RelativeColumn(2);   // Naziv
                            columns.ConstantColumn(40);  // J.M.
                            columns.ConstantColumn(55);  // Cena
                            columns.ConstantColumn(60);  // Ulaz Kol
                            columns.ConstantColumn(70);  // Ulaz Vred (Dug)
                            columns.ConstantColumn(60);  // Izlaz Kol
                            columns.ConstantColumn(70);  // Izlaz Vred (Pot)
                            columns.ConstantColumn(65);  // Saldo Kol
                            columns.ConstantColumn(75);  // Saldo Vred
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("R.b").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Magacin").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Šifra").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Naziv artikla / robe").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("J.M.").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Cena").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Ulaz Kol").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Duguje (Ulaz)").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Izlaz Kol").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Potražuje").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Stanje Kol").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(2).Text("Saldo RSD").Bold().AlignRight();
                        });

                        int rbr = 1;
                        foreach (var st in stavke)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(rbr.ToString());
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(st.SifraMagacina);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(st.SifraArtikla).Bold();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(st.NazivArtikla);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text(st.JedinicaMere);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.Cena:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.UlazKolicina:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.UlazVrednost:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.IzlazKolicina:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.IzlazVrednost:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.SaldoKolicinski:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(2).Text($"{st.SaldoVrednosni:N2}").AlignRight();
                            rbr++;
                        }
                    });

                    decimal ukDug = stavke.Sum(s => s.UlazVrednost);
                    decimal ukPot = stavke.Sum(s => s.IzlazVrednost);
                    decimal ukSal = stavke.Sum(s => s.SaldoVrednosni);

                    col.Item().PaddingTop(10).AlignRight().Width(380).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(120); });
                        t.Cell().Text("UKUPNO DUGUJE (ULAZ):").Bold();
                        t.Cell().Text($"{ukDug:N2} RSD").AlignRight();
                        t.Cell().Text("UKUPNO POTRAŽUJE (IZLAZ):").Bold();
                        t.Cell().Text($"{ukPot:N2} RSD").AlignRight();
                        t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("UKUPAN SALDO ZALIHA:").Bold().FontSize(10);
                        t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text($"{ukSal:N2} RSD").Bold().FontSize(10).AlignRight();
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

    public static byte[] GenerisiRobnuKarticuPdf(Firma firma, Magacin magacin, Artikal artikal, List<MaterijalnaKartica> kartice)
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
                        col.Item().Text("ROBNO - MATERIJALNA KARTICA").Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
                        col.Item().Text($"Magacin: {magacin.NazivMagacina} ({magacin.SifraMagacina})").Bold().FontSize(10);
                        col.Item().Text($"Artikal: {artikal.Naziv} ({artikal.SifraArtikla})").Bold().FontSize(10);
                        col.Item().Text($"J.M.: {artikal.JedinicaMere} | Prod. cena: {artikal.ProdajnaCena:N2} RSD");
                    });
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);  // Rbr
                            columns.ConstantColumn(65);  // Datum
                            columns.RelativeColumn(2);   // Opis
                            columns.ConstantColumn(55);  // Ulaz
                            columns.ConstantColumn(55);  // Izlaz
                            columns.ConstantColumn(60);  // Stanje
                            columns.ConstantColumn(65);  // Cena
                            columns.ConstantColumn(75);  // Saldo RSD
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("R.br").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Datum").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Opis promene").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Ulaz").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Izlaz").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Stanje").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Cena").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).Text("Saldo RSD").Bold().AlignRight();
                        });

                        int rbr = 1;
                        foreach (var st in kartice)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(rbr.ToString());
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.DatumPromene:dd.MM.yyyy}");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text(st.OpisPromene ?? "");
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.Ulaz:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.Izlaz:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.Stanje:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.Cena:N2}").AlignRight();
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(3).Text($"{st.Saldo:N2}").AlignRight();
                            rbr++;
                        }
                    });

                    decimal ukUlaz = kartice.Sum(k => k.Ulaz);
                    decimal ukIzlaz = kartice.Sum(k => k.Izlaz);
                    decimal zadnjeStanje = kartice.LastOrDefault()?.Stanje ?? 0m;
                    decimal zadnjiSaldo = kartice.LastOrDefault()?.Saldo ?? 0m;

                    col.Item().PaddingTop(12).AlignRight().Width(280).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(120); });
                        t.Cell().Text("Ukupan ulaz:").Bold();
                        t.Cell().Text($"{ukUlaz:N2} {artikal.JedinicaMere}").AlignRight();
                        t.Cell().Text("Ukupan izlaz:").Bold();
                        t.Cell().Text($"{ukIzlaz:N2} {artikal.JedinicaMere}").AlignRight();
                        t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("KRAJNJE STANJE:").Bold().FontSize(10);
                        t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text($"{zadnjeStanje:N2} {artikal.JedinicaMere}").Bold().FontSize(10).AlignRight();
                        t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("KRAJNJI SALDO:").Bold().FontSize(10);
                        t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text($"{zadnjiSaldo:N2} RSD").Bold().FontSize(10).AlignRight();
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
