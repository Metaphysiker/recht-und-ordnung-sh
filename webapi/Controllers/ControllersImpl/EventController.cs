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
/// Controller for managing Event entities
/// </summary>
[Authorize]
[Route("api/[controller]")]
public class EventController : ModelDtoControllerBase<Event, EventDto, EventSearchDto>
{
    public EventController(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Map DTO to Entity
    /// </summary>
    protected override Event MapToEntity(EventDto dto, Event? existingEntity = null)
    {
        var entity = existingEntity ?? new Event();

        entity.EventDate = dto.EventDate.ToUniversalTime();
        entity.Title = dto.Title;
        entity.Description = dto.Description;
        entity.ProblemId = dto.ProblemId;
        entity.UserId = dto.UserId;

        return entity;
    }

    /// <summary>
    /// Map Entity to DTO
    /// </summary>
    protected override EventDto MapToDto(Event entity)
    {
        return new EventDto
        {
            Id = entity.Id,
            EventDate = entity.EventDate,
            Title = entity.Title,
            Description = entity.Description,
            ProblemId = entity.ProblemId,
            UserId = entity.UserId
        };
    }

    /// <summary>
    /// Override to implement custom search logic
    /// </summary>
    protected override IQueryable<Event> ApplySearchFilter(IQueryable<Event> query, EventSearchDto search)
    {
        if (!string.IsNullOrWhiteSpace(search.SearchTerm))
        {
            query = query.Where(e =>
                e.Title.Contains(search.SearchTerm) ||
                e.Description.Contains(search.SearchTerm));
        }

        if (search.ProblemId.HasValue)
        {
            query = query.Where(e => e.ProblemId == search.ProblemId.Value);
        }

        if (search.FromDate.HasValue)
        {
            query = query.Where(e => e.EventDate >= search.FromDate.Value);
        }

        if (search.ToDate.HasValue)
        {
            query = query.Where(e => e.EventDate <= search.ToDate.Value);
        }

        return query;
    }
}
