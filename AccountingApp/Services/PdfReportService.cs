using AccountingData.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AccountingApp.Services;

public class PdfReportService
{
    public static byte[] GenerisiDnevnikPdf(Firma firma, List<Nalog> nalozi)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

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
                            columns.ConstantColumn(90);
                            columns.ConstantColumn(90);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Nalog").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Datum").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Dokument / Opis").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Konto").Bold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Duguje (RSD)").Bold().AlignRight();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Potražuje (RSD)").Bold().AlignRight();
                        });

                        decimal zbirDuguje = 0;
                        decimal zbirPotrazuje = 0;

                        foreach (var nalog in nalozi)
                        {
                            foreach (var st in nalog.Stavke)
                            {
                                zbirDuguje += st.Duguje;
                                zbirPotrazuje += st.Potrazuje;

                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(nalog.BrojNaloga);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(nalog.DatumNaloga.ToString("dd.MM.yyyy"));
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(st.Opis ?? nalog.Opis ?? "");
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(st.BrojKonta);
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{st.Duguje:N2}").AlignRight();
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{st.Potrazuje:N2}").AlignRight();
                            }
                        }

                        table.Cell().ColumnSpan(4).Padding(6).Text("UKUPAN PROMET DNEVNIKA:").Bold().AlignRight();
                        table.Cell().Padding(6).Text($"{zbirDuguje:N2}").Bold().AlignRight();
                        table.Cell().Padding(6).Text($"{zbirPotrazuje:N2}").Bold().AlignRight();
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
