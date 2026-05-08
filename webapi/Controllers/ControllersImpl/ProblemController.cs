using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webapi.Controllers;
using webapi.Data;
using webapi.Models.DTOs.DTOsImpl;
using webapi.Models.ModelsImpl;
using QuestPDF.Fluent;
using webapi.Pdf;
using webapi.Search;
using webapi.Services;

namespace webapi.Controllers.ControllersImpl;

[Authorize]
[Route("api/[controller]")]
public class ProblemController : ModelDtoControllerBase<Problem, ProblemDto, ProblemSearchDto>
{
    private readonly IS3Service _s3;
    private readonly ILogger<ProblemController> _logger;
    private readonly PasswordHasher<Problem> _hasher = new();

    public ProblemController(ApplicationDbContext context, IS3Service s3, ILogger<ProblemController> logger) : base(context)
    {
        _s3 = s3;
        _logger = logger;
    }

    protected override Problem MapToEntity(ProblemDto dto, Problem? existingEntity = null)
    {
        var entity = existingEntity ?? new Problem();

        entity.Title = dto.Title;
        entity.Description = dto.Description;
        entity.UserId = dto.UserId;
        entity.Coordinates = dto.Coordinates
            .Select(c => new Coordinate { Latitude = c.Latitude, Longitude = c.Longitude })
            .ToList();

        if (!string.IsNullOrWhiteSpace(dto.PublicPassword))
            entity.PublicPassword = _hasher.HashPassword(entity, dto.PublicPassword);
        else if (dto.PublicPassword == string.Empty)
            entity.PublicPassword = null;

        return entity;
    }

    protected override ProblemDto MapToDto(Problem entity)
    {
        return new ProblemDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            EventIds = entity.Events.Select(e => e.Id).ToList(),
            Coordinates = entity.Coordinates
                .Select(c => new CoordinateDto { Latitude = c.Latitude, Longitude = c.Longitude })
                .ToList(),
            UserId = entity.UserId,
            HasPublicPassword = entity.PublicPassword != null
        };
    }

    protected override IQueryable<Problem> ApplySearchFilter(IQueryable<Problem> query, ProblemSearchDto search)
    {
        query = query.Include(p => p.Events);

        if (!string.IsNullOrWhiteSpace(search.SearchTerm))
        {
            query = query.Where(p =>
                p.Title.Contains(search.SearchTerm) ||
                p.Description.Contains(search.SearchTerm));
        }

        return query;
    }

    public override async Task<ActionResult<ProblemDto>> GetById(Guid id)
    {
        var entity = await _dbSet
            .Include(p => p.Events)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (entity == null)
            return NotFound(new { message = $"Entity with ID {id} not found" });

        return Ok(MapToDto(entity));
    }

    [AllowAnonymous]
    [HttpPost("{id}/unlock")]
    public async Task<IActionResult> Unlock(Guid id, [FromBody] UnlockRequest request)
    {
        var problem = await _dbSet
            .Include(p => p.Events)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (problem == null)
            return NotFound();

        if (problem.PublicPassword == null)
            return Forbid();

        var result = _hasher.VerifyHashedPassword(problem, problem.PublicPassword, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized();

        var events = problem.Events
            .OrderBy(e => e.EventDate)
            .Select(e => new EventDto
            {
                Id = e.Id,
                EventDate = e.EventDate,
                Title = e.Title,
                Description = e.Description,
                ProblemId = e.ProblemId,
                UserId = e.UserId
            })
            .ToList();

        return Ok(new { Problem = MapToDto(problem), Events = events });
    }

    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> GetPdf(Guid id)
    {
        var problem = await _dbSet
            .Include(p => p.Events)
            .Include(p => p.Attachments)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (problem == null)
            return NotFound();

        return await BuildPdfResponse(problem);
    }

    [AllowAnonymous]
    [HttpPost("{id}/pdf-public")]
    public async Task<IActionResult> GetPdfPublic(Guid id, [FromBody] UnlockRequest request)
    {
        var problem = await _dbSet
            .Include(p => p.Events)
            .Include(p => p.Attachments)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (problem == null)
            return NotFound();

        if (problem.PublicPassword == null)
            return Forbid();

        var result = _hasher.VerifyHashedPassword(problem, problem.PublicPassword, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized();

        return await BuildPdfResponse(problem);
    }

    private async Task<IActionResult> BuildPdfResponse(Problem problem)
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

        var document = new ProblemPdfDocument(problem, events, mapImage, images);
        var pdfBytes = document.GeneratePdf();

        var filename = $"{problem.Title.Replace(" ", "_")}.pdf";
        return File(pdfBytes, "application/pdf", filename);
    }

    private static byte[] ReadToBytes(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".tiff", ".tif" };

    private static bool IsImageFile(string fileName) =>
        ImageExtensions.Contains(Path.GetExtension(fileName));
}

public record UnlockRequest(string Password);
