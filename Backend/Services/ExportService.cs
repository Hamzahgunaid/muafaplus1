using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MuafaPlus.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MuafaPlus.Services;

/// <summary>
/// Generates Arabic RTL export files (PDF and Word) from generated article content.
///
/// PDF:  QuestPDF with right-to-left text alignment.
/// Word: DocumentFormat.OpenXml with BiDi (bidirectional) paragraph and run properties,
///       Sakkal Majalla font, and right-to-left section direction.
/// </summary>
public class ExportService
{
    private readonly ILogger<ExportService> _logger;

    public ExportService(ILogger<ExportService> logger)
    {
        _logger = logger;
    }

    // ── PDF ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a multi-article Arabic RTL PDF for a session.
    /// Returns the PDF bytes ready to stream to the client.
    /// </summary>
    public byte[] GenerateSessionPdf(SessionExportData data)
    {
        _logger.LogInformation("Generating PDF — session:{S} articles:{C}",
            data.SessionId, data.Articles.Count);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x
                    .FontFamily("Noto Sans Arabic", "Arial")
                    .FontSize(11)
                    .DirectionFromRightToLeft());

                // Cover header
                page.Header().Element(header =>
                {
                    header.Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().AlignRight().Text("معافى+")
                                .Bold().FontSize(18).FontColor(Colors.Teal.Medium);
                            col.Item().AlignRight().Text("برنامج التثقيف الصحي — تقرير المريض")
                                .FontSize(10).FontColor(Colors.Grey.Medium);
                        });
                    });
                    header.Row(row =>
                    {
                        row.RelativeItem().PaddingTop(4).BorderBottom(0.5f).BorderColor(Colors.Teal.Lighten2).Text(" ");
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("صفحة ").FontSize(9).FontColor(Colors.Grey.Medium);
                    x.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                    x.Span(" من ").FontSize(9).FontColor(Colors.Grey.Medium);
                    x.TotalPages().FontSize(9).FontColor(Colors.Grey.Medium);
                });

                page.Content().Column(col =>
                {
                    // Session meta box
                    col.Item().PaddingBottom(12).Background(Colors.Teal.Lighten5).Padding(10)
                        .Border(0.5f).BorderColor(Colors.Teal.Lighten3).Column(meta =>
                    {
                        meta.Item().AlignRight().Text($"رقم الجلسة: {data.SessionId[..8]}…")
                            .FontSize(9).FontColor(Colors.Grey.Medium);
                        meta.Item().AlignRight().Text($"مستوى الخطر: {RiskLevelAr(data.RiskLevel)}")
                            .FontSize(10).Bold().FontColor(RiskColor(data.RiskLevel));
                        meta.Item().AlignRight().Text($"عدد المقالات: {data.Articles.Count}")
                            .FontSize(9).FontColor(Colors.Grey.Medium);
                    });

                    // Articles
                    foreach (var article in data.Articles)
                    {
                        col.Item().PageBreak();

                        // Article type label
                        col.Item().PaddingBottom(6)
                            .Background(article.ArticleType == "summary" ? Colors.Teal.Lighten4 : Colors.Blue.Lighten5)
                            .Padding(6)
                            .AlignRight()
                            .Text(article.ArticleType == "summary" ? "المقال التلخيصي" : "مقال تفصيلي")
                            .FontSize(9).FontColor(Colors.Grey.Medium);

                        // Strip markdown for PDF rendering — render as plain Arabic text
                        var plainText = StripMarkdown(article.Content);

                        col.Item().AlignRight().Text(plainText)
                            .FontSize(11)
                            .LineHeight(1.75f)
                            .DirectionFromRightToLeft();

                        if (!string.IsNullOrEmpty(article.CoverageCodes))
                        {
                            col.Item().PaddingTop(8).AlignRight()
                                .Text($"كود التغطية: {article.CoverageCodes}")
                                .FontSize(9).FontColor(Colors.Grey.Medium).Italic();
                        }
                    }
                });
            });
        });

        return document.GeneratePdf();
    }

    // ── Word (.docx) ──────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a multi-article Arabic RTL Word document.
    /// Uses OpenXml BiDi properties and Sakkal Majalla / Noto Sans Arabic font.
    /// Returns the docx bytes ready to stream to the client.
    /// </summary>
    public byte[] GenerateSessionDocx(SessionExportData data)
    {
        _logger.LogInformation("Generating DOCX — session:{S}", data.SessionId);

        using var ms     = new MemoryStream();
        using var wordDoc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document);

        var mainPart = wordDoc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        var body = mainPart.Document.Body!;

        // Document-level RTL: set section properties
        var sectPr = new SectionProperties(
            new BiDi(),
            new PageMargin { Top = 1134, Bottom = 1134, Left = 1134, Right = 1134 }
        );
        body.AppendChild(sectPr);

        // ── Title paragraph ───────────────────────────────────────────────────
        body.InsertBefore(
            ArabicParagraph("معافى+ — تقرير المريض", fontSize: 28, bold: true, center: true),
            sectPr);

        body.InsertBefore(
            ArabicParagraph($"رقم الجلسة: {data.SessionId[..8]}…  |  مستوى الخطر: {RiskLevelAr(data.RiskLevel)}",
                fontSize: 20, bold: false, center: true, color: "595959"),
            sectPr);

        body.InsertBefore(HorizontalRule(), sectPr);

        // ── Articles ──────────────────────────────────────────────────────────
        foreach (var article in data.Articles)
        {
            // Article type heading
            var typeLabel = article.ArticleType == "summary" ? "المقال التلخيصي" : "مقال تفصيلي";
            body.InsertBefore(ArabicParagraph(typeLabel, fontSize: 22, bold: true, color: "0F6E56"), sectPr);

            if (!string.IsNullOrEmpty(article.CoverageCodes))
                body.InsertBefore(
                    ArabicParagraph($"التغطية: {article.CoverageCodes}", fontSize: 18, color: "888780"),
                    sectPr);

            // Article body — split on newlines, one paragraph per line
            var lines = article.Content
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0);

            foreach (var line in lines)
            {
                // Markdown heading detection → bold paragraph
                if (line.StartsWith("###"))
                    body.InsertBefore(ArabicParagraph(line.TrimStart('#').Trim(), fontSize: 20, bold: true), sectPr);
                else if (line.StartsWith("##"))
                    body.InsertBefore(ArabicParagraph(line.TrimStart('#').Trim(), fontSize: 22, bold: true), sectPr);
                else if (line.StartsWith("#"))
                    body.InsertBefore(ArabicParagraph(line.TrimStart('#').Trim(), fontSize: 24, bold: true), sectPr);
                else if (line.StartsWith("**") && line.EndsWith("**"))
                    body.InsertBefore(ArabicParagraph(line.Trim('*'), fontSize: 20, bold: true), sectPr);
                else if (line.StartsWith("- ") || line.StartsWith("* "))
                    body.InsertBefore(ArabicParagraph("• " + line[2..], fontSize: 20, indent: 360), sectPr);
                else
                    body.InsertBefore(ArabicParagraph(line, fontSize: 20), sectPr);
            }

            body.InsertBefore(HorizontalRule(), sectPr);
        }

        wordDoc.Save();
        return ms.ToArray();
    }

    // ── OpenXml helpers ───────────────────────────────────────────────────────

    private static Paragraph ArabicParagraph(
        string text,
        int fontSize = 20,
        bool bold    = false,
        bool center  = false,
        string? color = null,
        int indent   = 0)
    {
        var runProps = new RunProperties(
            new RunFonts { Ascii = "Sakkal Majalla", HighAnsi = "Sakkal Majalla", ComplexScript = "Sakkal Majalla" },
            new FontSize { Val = fontSize.ToString() },
            new FontSizeComplexScript { Val = fontSize.ToString() },
            new RightToLeftText()
        );
        if (bold)  runProps.AppendChild(new Bold());
        if (color != null) runProps.AppendChild(new Color { Val = color });

        var paraProps = new ParagraphProperties(
            new BiDi(),
            new Justification { Val = center ? JustificationValues.Center : JustificationValues.Both }
        );
        if (indent > 0)
            paraProps.AppendChild(new Indentation { Right = indent.ToString() });

        return new Paragraph(
            paraProps,
            new Run(runProps, new Text(text) { Space = SpaceProcessingModeValues.Preserve })
        );
    }

    private static Paragraph HorizontalRule()
    {
        var paraProps = new ParagraphProperties();
        paraProps.AppendChild(new ParagraphBorder(
            new BottomBorder
            {
                Val   = BorderValues.Single,
                Size  = 6,
                Color = "9FE1CB"
            }
        ));
        return new Paragraph(paraProps);
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    private static string StripMarkdown(string text)
    {
        // Minimal markdown strip for PDF plain-text rendering
        return System.Text.RegularExpressions.Regex.Replace(text,
            @"(#{1,6}\s|(\*\*|__)(.*?)(\*\*|__)|(\*|_)(.*?)(\*|_)|\[([^\]]+)\]\([^)]+\)|`{1,3}[^`]*`{1,3})",
            m =>
            {
                var s = m.Value;
                if (s.StartsWith("#"))  return s.TrimStart('#').Trim();
                if (s.StartsWith("**") || s.StartsWith("__")) return m.Groups[3].Value;
                if (s.StartsWith("*")  || s.StartsWith("_"))  return m.Groups[6].Value;
                if (s.StartsWith("["))  return m.Groups[8].Value;
                return string.Empty;
            });
    }

    private static string RiskLevelAr(string? level) => level switch
    {
        "LOW"      => "منخفض",
        "MODERATE" => "متوسط",
        "HIGH"     => "مرتفع",
        "CRITICAL" => "حرج",
        _          => level ?? "غير محدد"
    };

    private static string RiskColor(string? level) => level switch
    {
        "LOW"      => Colors.Teal.Medium,
        "MODERATE" => Colors.Orange.Medium,
        "HIGH"     => Colors.DeepOrange.Medium,
        "CRITICAL" => Colors.Red.Medium,
        _          => Colors.Grey.Medium
    };
}

// ── Export data model ─────────────────────────────────────────────────────────

public class SessionExportData
{
    public string SessionId { get; set; } = string.Empty;
    public string? RiskLevel { get; set; }
    public List<ArticleExportItem> Articles { get; set; } = [];
}

public class ArticleExportItem
{
    public string ArticleType   { get; set; } = string.Empty;
    public string CoverageCodes { get; set; } = string.Empty;
    public string Content       { get; set; } = string.Empty;
}
