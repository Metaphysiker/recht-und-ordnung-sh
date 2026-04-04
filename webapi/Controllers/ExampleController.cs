using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webapi.Data;
using webapi.Models.DTOs;
using webapi.Models.ModelsImpl;

namespace webapi.Controllers;

/// <summary>
/// Example controller demonstrating how to use ModelDtoControllerBase
/// </summary>
[Authorize]
[Route("api/[controller]")]
public class ExampleController : ModelDtoControllerBase<ExampleEntity, ExampleDto, ExampleSearchDto>
{
    public ExampleController(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Map DTO to Entity
    /// </summary>
    protected override ExampleEntity MapToEntity(ExampleDto dto, ExampleEntity? existingEntity = null)
    {
        var entity = existingEntity ?? new ExampleEntity();

        entity.Title = dto.Title;
        entity.Description = dto.Description;

        return entity;
    }

    /// <summary>
    /// Map Entity to DTO
    /// </summary>
    protected override ExampleDto MapToDto(ExampleEntity entity)
    {
        return new ExampleDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description
        };
    }

    /// <summary>
    /// Override to implement custom search logic
    /// </summary>
    protected override IQueryable<ExampleEntity> ApplySearchFilter(IQueryable<ExampleEntity> query, ExampleSearchDto search)
    {
        if (!string.IsNullOrWhiteSpace(search.SearchTerm))
        {
            query = query.Where(e =>
                e.Title.Contains(search.SearchTerm) ||
                e.Description.Contains(search.SearchTerm));
        }

        return query;
    }
}
