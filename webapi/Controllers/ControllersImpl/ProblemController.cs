using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webapi.Controllers;
using webapi.Data;
using webapi.Models.DTOs.DTOsImpl;
using webapi.Models.ModelsImpl;
using webapi.Search;

namespace webapi.Controllers.ControllersImpl;

/// <summary>
/// Controller for managing Problem entities
/// </summary>
[Authorize]
[Route("api/[controller]")]
public class ProblemController : ModelDtoControllerBase<Problem, ProblemDto, ProblemSearchDto>
{
    public ProblemController(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Map DTO to Entity
    /// </summary>
    protected override Problem MapToEntity(ProblemDto dto, Problem? existingEntity = null)
    {
        var entity = existingEntity ?? new Problem();

        entity.Title = dto.Title;
        entity.Description = dto.Description;
        entity.UserId = dto.UserId;

        // Note: Event relationships are managed separately through the Event controller
        // The EventIds in the DTO are used for reading, not for writing

        return entity;
    }

    /// <summary>
    /// Map Entity to DTO
    /// </summary>
    protected override ProblemDto MapToDto(Problem entity)
    {
        return new ProblemDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            EventIds = entity.Events.Select(e => e.Id).ToList(),
            UserId = entity.UserId
        };
    }

    /// <summary>
    /// Override to implement custom search logic
    /// </summary>
    protected override IQueryable<Problem> ApplySearchFilter(IQueryable<Problem> query, ProblemSearchDto search)
    {
        // Include related Events for mapping
        query = query.Include(p => p.Events);

        if (!string.IsNullOrWhiteSpace(search.SearchTerm))
        {
            query = query.Where(p =>
                p.Title.Contains(search.SearchTerm) ||
                p.Description.Contains(search.SearchTerm));
        }

        return query;
    }

    /// <summary>
    /// Override GetById to include related Events
    /// </summary>
    public override async Task<ActionResult<ProblemDto>> GetById(Guid id)
    {
        var entity = await _dbSet
            .Include(p => p.Events)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (entity == null)
        {
            return NotFound(new { message = $"Entity with ID {id} not found" });
        }

        return Ok(MapToDto(entity));
    }
}
