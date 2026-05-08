using System.Net;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using webapi.Models.ModelsImpl;

namespace webapi.Pdf;

public class ProblemPdfDocument : IDocument
{
    private readonly Problem _problem;
    private readonly List<Event> _events;
    private readonly byte[]? _mapImage;
    private readonly List<(string FileName, byte[] Data)> _images;

    private static readonly Color PrimaryColor = Color.FromHex("#1565C0");
    private static readonly Color TextSecondary = Color.FromHex("#757575");
    private static readonly Color DividerColor = Color.FromHex("#E0E0E0");

    public ProblemPdfDocument(Problem problem, List<Event> events, byte[]? mapImage = null, List<(string FileName, byte[] Data)>? images = null)
    {
        _problem = problem;
        _events = events;
        _mapImage = mapImage;
        _images = images ?? [];
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

            page.Header().Element(ComposeHeader);
            page.Content().PaddingTop(16).Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container
            .BorderBottom(1)
            .BorderColor(DividerColor)
            .PaddingBottom(8)
            .Row(row =>
            {
                row.RelativeItem()
                    .Text("Recht und Ordnung - Schaffhausen")
                    .FontSize(10)
                    .FontColor(TextSecondary);

                row.RelativeItem()
                    .AlignRight()
                    .Text($"Erstellt: {DateTimeOffset.UtcNow:dd.MM.yyyy HH:mm} UTC")
                    .FontSize(10)
                    .FontColor(TextSecondary);
            });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(12);

            col.Item()
                .Text(_problem.Title)
                .FontSize(22)
                .Bold()
                .FontColor(PrimaryColor);

            if (!string.IsNullOrWhiteSpace(_problem.Description))
            {
                col.Item()
                    .Text(StripHtml(_problem.Description))
                    .FontSize(11)
                    .FontColor(TextSecondary);
            }

            if (_mapImage != null)
            {
                col.Item()
                    .Border(1)
                    .BorderColor(DividerColor)
                    .Image(_mapImage)
                    .FitWidth();
            }

            col.Item().PaddingTop(4).LineHorizontal(1).LineColor(DividerColor);

            col.Item()
                .Text("Ereignisse")
                .FontSize(14)
                .Bold();

            if (_events.Count == 0)
            {
                col.Item()
                    .Text("Keine Ereignisse für dieses Problem erfasst.")
                    .FontColor(TextSecondary);
            }
            else
            {
                foreach (var evt in _events)
                {
                    col.Item().Element(c => ComposeEvent(c, evt));
                }
            }

            if (_images.Count > 0)
            {
                col.Item().PaddingTop(4).LineHorizontal(1).LineColor(DividerColor);

                col.Item()
                    .Text("Bilder")
                    .FontSize(14)
                    .Bold();

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                    });

                    for (int i = 0; i < _images.Count; i += 2)
                    {
                        AddImageCell(table, _images[i]);
                        if (i + 1 < _images.Count)
                            AddImageCell(table, _images[i + 1]);
                        else
                            table.Cell();
                    }
                });
            }
        });
    }

    private static void AddImageCell(TableDescriptor table, (string FileName, byte[] Data) image)
    {
        table.Cell().Padding(4).Column(col =>
        {
            col.Item()
                .Border(1)
                .BorderColor(Color.FromHex("#E0E0E0"))
                .Image(image.Data)
                .FitWidth();
            col.Item()
                .PaddingTop(2)
                .Text(image.FileName)
                .FontSize(8)
                .FontColor(Color.FromHex("#757575"));
        });
    }

    private static void ComposeEvent(IContainer container, Event evt)
    {
        container
            .ShowEntire()
            .BorderLeft(3)
            .BorderColor(PrimaryColor)
            .PaddingLeft(12)
            .PaddingVertical(8)
            .Column(col =>
            {
                col.Spacing(3);

                col.Item()
                    .Text(evt.EventDate.ToString("dd.MM.yyyy HH:mm") + " UTC")
                    .FontSize(9)
                    .FontColor(TextSecondary);

                col.Item()
                    .Text(evt.Title)
                    .FontSize(12)
                    .Bold();

                if (!string.IsNullOrWhiteSpace(evt.Description))
                {
                    col.Item()
                        .Text(StripHtml(evt.Description))
                        .FontSize(10)
                        .FontColor(TextSecondary);
                }
            });
    }

    private void ComposeFooter(IContainer container)
    {
        container
            .BorderTop(1)
            .BorderColor(DividerColor)
            .PaddingTop(8)
            .AlignRight()
            .Text(x =>
            {
                x.Span("Seite ").FontSize(9).FontColor(TextSecondary);
                x.CurrentPageNumber().FontSize(9).FontColor(TextSecondary);
                x.Span(" von ").FontSize(9).FontColor(TextSecondary);
                x.TotalPages().FontSize(9).FontColor(TextSecondary);
            });
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;

        html = Regex.Replace(html, @"<br\s*/?>|</p>|</div>|</h[1-6]>", "\n");
        html = Regex.Replace(html, @"<li[^>]*>", "• ");
        html = Regex.Replace(html, @"</li>", "\n");
        html = Regex.Replace(html, @"<[^>]+>", "");
        html = WebUtility.HtmlDecode(html);
        html = Regex.Replace(html, @"\n{3,}", "\n\n");

        return html.Trim();
    }
}
