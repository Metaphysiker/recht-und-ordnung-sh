using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webapi.Controllers;
using webapi.Data;
using webapi.Models;
using webapi.Models.DTOs.DTOsImpl;
using webapi.Models.ModelsImpl;
using webapi.Search;
using webapi.Services;

namespace webapi.Controllers.ControllersImpl;

[Authorize]
[Route("api/[controller]")]
public class AttachmentController : ModelDtoControllerBase<Attachment, AttachmentDto, AttachmentSearchDto>
{
    private readonly IS3Service _s3;

    public AttachmentController(ApplicationDbContext context, IS3Service s3) : base(context)
    {
        _s3 = s3;
    }

    protected override Attachment MapToEntity(AttachmentDto dto, Attachment? existingEntity = null)
    {
        var entity = existingEntity ?? new Attachment();
        entity.ProblemId = dto.ProblemId;
        entity.EventId = dto.EventId;
        entity.UserId = dto.UserId;
        return entity;
    }

    protected override AttachmentDto MapToDto(Attachment entity)
    {
        return new AttachmentDto
        {
            Id = entity.Id,
            FileName = entity.FileName,
            ContentType = entity.ContentType,
            S3Key = entity.S3Key,
            FileSizeBytes = entity.FileSizeBytes,
            ProblemId = entity.ProblemId,
            EventId = entity.EventId,
            UserId = entity.UserId,
        };
    }

    protected override IQueryable<Attachment> ApplySearchFilter(IQueryable<Attachment> query, AttachmentSearchDto search)
    {
        if (search.ProblemId.HasValue)
            query = query.Where(a => a.ProblemId == search.ProblemId.Value);
        if (search.EventId.HasValue)
            query = query.Where(a => a.EventId == search.EventId.Value);
        return query;
    }

    // Attachments are created via /upload, not JSON body
    public override Task<ActionResult<AttachmentDto>> Create([FromBody] AttachmentDto dto)
        => Task.FromResult<ActionResult<AttachmentDto>>(StatusCode(405));

    [AllowAnonymous]
    [HttpPost("upload")]
    public async Task<ActionResult<AttachmentDto>> Upload(
        [FromForm] IFormFile file,
        [FromForm] Guid parentId,
        [FromForm] ParentType parentType,
        [FromForm] Guid? userId)
    {
        var id = Guid.NewGuid();
        var ext = Path.GetExtension(file.FileName);
        var key = $"attachments/{id}{ext}";
        var contentType = ResolveContentType(file.ContentType, file.FileName);

        using var stream = file.OpenReadStream();
        await _s3.UploadAsync(key, stream, contentType);

        var entity = new Attachment
        {
            Id = id,
            FileName = file.FileName,
            ContentType = contentType,
            S3Key = key,
            FileSizeBytes = file.Length,
            ProblemId = parentType == ParentType.Problem ? parentId : null,
            EventId = parentType == ParentType.Event ? parentId : null,
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _dbSet.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, MapToDto(entity));
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity == null) return NotFound();

        var stream = await _s3.DownloadAsync(entity.S3Key);
        var contentType = string.IsNullOrWhiteSpace(entity.ContentType) ? "application/octet-stream" : entity.ContentType;
        return File(stream, contentType, entity.FileName);
    }

    [AllowAnonymous]
    [HttpPost("{id}/download-public")]
    public async Task<IActionResult> DownloadPublic(Guid id, [FromBody] PublicAttachmentDownloadRequest request)
    {
        var entity = await _dbSet
            .Include(a => a.Problem)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (entity == null) return NotFound();

        var problem = entity.Problem;
        if (problem == null || problem.PublicPassword == null) return Forbid();

        var hasher = new PasswordHasher<Problem>();
        var result = hasher.VerifyHashedPassword(problem, problem.PublicPassword, request.Password);
        if (result == PasswordVerificationResult.Failed) return Unauthorized();

        var stream = await _s3.DownloadAsync(entity.S3Key);
        var contentType = string.IsNullOrWhiteSpace(entity.ContentType) ? "application/octet-stream" : entity.ContentType;
        return File(stream, contentType, entity.FileName);
    }

    public override async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity == null)
            return NotFound(new { message = $"Entity with ID {id} not found" });

        await _s3.DeleteAsync(entity.S3Key);
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static readonly Dictionary<string, string> ExtensionMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".jpg",  "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png",  "image/png"  },
        { ".gif",  "image/gif"  },
        { ".webp", "image/webp" },
        { ".bmp",  "image/bmp"  },
        { ".tiff", "image/tiff" },
        { ".tif",  "image/tiff" },
        { ".pdf",  "application/pdf" },
    };

    private static string ResolveContentType(string? browserType, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(browserType) && browserType != "application/octet-stream")
            return browserType;

        var ext = Path.GetExtension(fileName);
        return ExtensionMimeTypes.TryGetValue(ext, out var mime) ? mime : "application/octet-stream";
    }
}

public record PublicAttachmentDownloadRequest(string Password);
