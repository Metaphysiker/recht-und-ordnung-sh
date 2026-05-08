using QuestPDF.Fluent;
using webapi.Models.ModelsImpl;
using webapi.Services;

namespace webapi.Pdf;

public class ProblemPdfGenerator
{
    private readonly IS3Service _s3;
    private readonly ILogger<ProblemPdfGenerator> _logger;

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".tiff", ".tif" };

    public ProblemPdfGenerator(IS3Service s3, ILogger<ProblemPdfGenerator> logger)
    {
        _s3 = s3;
        _logger = logger;
    }

    public async Task<(byte[] Bytes, string Filename)> GenerateAsync(Problem problem)
    {
        var events = problem.Events.OrderBy(e => e.EventDate).ToList();

        byte[]? mapImage = null;
        if (problem.Coordinates.Count > 0)
            mapImage = await MapImageGenerator.GenerateAsync(problem.Coordinates);

        _logger.LogInformation("PDF: problem has {Count} attachments: {Types}",
            problem.Attachments.Count,
            string.Join(", ", problem.Attachments.Select(a => $"{a.FileName}={a.ContentType}")));

        var imageAttachments = problem.Attachments
            .Where(a => a.ContentType.StartsWith("image/") || IsImageFile(a.FileName))
            .OrderBy(a => a.CreatedAt)
            .ToList();

        _logger.LogInformation("PDF: {Count} image attachments after filter", imageAttachments.Count);

        var images = (await Task.WhenAll(
            imageAttachments.Select(async a =>
            {
                using var stream = await _s3.DownloadAsync(a.S3Key);
                var data = ReadToBytes(stream);
                _logger.LogInformation("PDF: downloaded {File} — {Bytes} bytes", a.FileName, data.Length);
                return (a.FileName, Data: data);
            })
        )).ToList();

        var pdfBytes = new ProblemPdfDocument(problem, events, mapImage, images).GeneratePdf();
        var filename = $"{problem.Title.Replace(" ", "_")}.pdf";

        return (pdfBytes, filename);
    }

    private static bool IsImageFile(string fileName) =>
        ImageExtensions.Contains(Path.GetExtension(fileName));

    private static byte[] ReadToBytes(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
