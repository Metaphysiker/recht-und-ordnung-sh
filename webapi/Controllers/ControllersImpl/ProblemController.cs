using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webapi.Controllers;
using webapi.Data;
using webapi.Models.DTOs;
using webapi.Models.DTOs.DTOsImpl;
using webapi.Models.ModelsImpl;
using webapi.Pdf;
using webapi.Search;


namespace webapi.Controllers.ControllersImpl;

[Authorize]
[Route("api/[controller]")]
public class ProblemController : ModelDtoControllerBase<Problem, ProblemDto, ProblemSearchDto>
{
    private readonly ProblemPdfGenerator _pdfGenerator;
    private readonly PasswordHasher<Problem> _hasher = new();

    public ProblemController(ApplicationDbContext context, ProblemPdfGenerator pdfGenerator) : base(context)
    {
        _pdfGenerator = pdfGenerator;
    }

    protected override Problem MapToEntity(ProblemDto dto, Problem? existingEntity = null)
    {
        var entity = existingEntity ?? new Problem();

        entity.Title = dto.Title;
        entity.Description = dto.Description;
        entity.UserId = dto.UserId;
        entity.Email = dto.Email;
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
            HasPublicPassword = entity.PublicPassword != null,
            Email = entity.Email
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
    [HttpPost("public")]
    public async Task<ActionResult<ProblemDto>> CreatePublic([FromBody] PublicProblemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Title is required" });

        var entity = new Problem
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description ?? string.Empty,
            Email = request.Email,
            Coordinates = (request.Coordinates ?? [])
                .Select(c => new Coordinate { Latitude = c.Latitude, Longitude = c.Longitude })
                .ToList(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _dbSet.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, MapToDto(entity));
    }

    [AllowAnonymous]
    [HttpPost("{id}/unlock")]
    public async Task<IActionResult> Unlock(Guid id, [FromBody] UnlockRequest request)
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

        var attachments = problem.Attachments
            .OrderBy(a => a.CreatedAt)
            .Select(a => new AttachmentDto
            {
                Id = a.Id,
                FileName = a.FileName,
                ContentType = a.ContentType,
                FileSizeBytes = a.FileSizeBytes,
            })
            .ToList();

        return Ok(new { Problem = MapToDto(problem), Events = events, Attachments = attachments });
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

        var (bytes, filename) = await _pdfGenerator.GenerateAsync(problem);
        return File(bytes, "application/pdf", filename);
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

        var (bytes, filename) = await _pdfGenerator.GenerateAsync(problem);
        return File(bytes, "application/pdf", filename);
    }
}

public record UnlockRequest(string Password);

public record PublicProblemRequest(
    string Title,
    string? Description,
    string? Email,
    List<CoordinateDto>? Coordinates);
