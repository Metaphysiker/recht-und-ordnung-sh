using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using webapi.Data;
using webapi.Models;
using webapi.Models.DTOs;
using webapi.Search;

namespace webapi.Controllers;

/// <summary>
/// Base controller providing CRUD operations for entities implementing IModel with DTOs.
///
/// Usage:
/// 1. Create an entity class implementing IModel (e.g., MyEntity : IModel)
/// 2. Create a DTO class implementing IDto (e.g., MyDto : IDto)
/// 3. Create a search DTO class extending Search (e.g., MySearchDto : Search)
/// 4. Create a controller inheriting from ModelDtoControllerBase:
///    [Route("api/[controller]")]
///    public class MyController : ModelDtoControllerBase&lt;MyEntity, MyDto, MySearchDto&gt;
///    {
///        public MyController(ApplicationDbContext context) : base(context) { }
///        protected override MyEntity MapToEntity(MyDto dto, MyEntity? existingEntity = null) { ... }
///        protected override MyDto MapToDto(MyEntity entity) { ... }
///
///        // Optional: Override to implement search functionality
///        protected override IQueryable&lt;MyEntity&gt; ApplySearchFilter(IQueryable&lt;MyEntity&gt; query, MySearchDto search)
///        {
///            var filtered = query;
///            if (!string.IsNullOrEmpty(search.SearchTerm))
///                filtered = filtered.Where(e =&gt; e.Name.Contains(search.SearchTerm));
///            return filtered;
///        }
///    }
///
/// Note: Ordering is handled automatically using dynamic LINQ.
/// Just specify "orderBy": "PropertyName" and "orderDirection": "Ascending" or "Descending" in the search request.
///
/// Provides endpoints:
/// - POST /api/controller/search - Search with pagination
/// - GET /api/controller/{id} - Get by ID
/// - POST /api/controller - Create new entity
/// - PUT /api/controller/{id} - Update existing entity
/// - DELETE /api/controller/{id} - Delete entity
/// </summary>
/// <typeparam name="TModel">The entity type implementing IModel</typeparam>
/// <typeparam name="TDto">The data transfer object type implementing IDto</typeparam>
/// <typeparam name="TSearch">The search DTO type extending Search</typeparam>
[ApiController]
public abstract class ModelDtoControllerBase<TModel, TDto, TSearch> : ControllerBase
    where TModel : class, IModel, new()
    where TDto : class, IDto
    where TSearch : Search.Search
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<TModel> _dbSet;

    protected ModelDtoControllerBase(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TModel>();
    }

    /// <summary>
    /// Maps a DTO to an entity. Must be implemented by derived controllers.
    /// </summary>
    protected abstract TModel MapToEntity(TDto dto, TModel? existingEntity = null);

    /// <summary>
    /// Maps an entity to a DTO. Must be implemented by derived controllers.
    /// </summary>
    protected abstract TDto MapToDto(TModel entity);

    /// <summary>
    /// Search entities with pagination
    /// </summary>
    [HttpPost("search")]
    public virtual async Task<ActionResult<PaginationDto<TDto>>> Search([FromBody] TSearch search)
    {
        var baseQuery = _dbSet.AsQueryable();

        // Apply search filter
        baseQuery = ApplySearchFilter(baseQuery, search);

        // Get total count before pagination
        var total = await baseQuery.CountAsync();

        // Default pagination values
        var page = search.Page ?? 0;
        var pageSize = search.PageSize ?? 10;

        // Apply ordering
        baseQuery = ApplyOrdering(baseQuery, search);

        // Apply pagination
        var items = await baseQuery
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PaginationDto<TDto>
        {
            Items = items.Select(MapToDto),
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return Ok(result);
    }

    /// <summary>
    /// Apply ordering to query. Override in derived classes to implement custom ordering logic.
    /// Default implementation uses dynamic LINQ to order by the specified property, or CreatedAt if not specified.
    /// </summary>
    /// <param name="query">The base query</param>
    /// <param name="search">The search criteria containing OrderBy and OrderDirection</param>
    /// <returns>Ordered query</returns>
    protected virtual IQueryable<TModel> ApplyOrdering(IQueryable<TModel> query, TSearch search)
    {
        var orderBy = search.OrderBy;
        var direction = search.OrderDirection == OrderDirection.Descending ? "DESC" : "ASC";

        // If no OrderBy specified, default to CreatedAt
        if (string.IsNullOrWhiteSpace(orderBy))
        {
            orderBy = nameof(IModel.CreatedAt);
        }

        // Use dynamic LINQ to order by the specified property
        try
        {
            return query.OrderBy($"{orderBy} {direction}");
        }
        catch
        {
            // Fallback to CreatedAt if the property doesn't exist
            return query.OrderBy($"{nameof(IModel.CreatedAt)} {direction}");
        }
    }

    /// <summary>
    /// Apply search filter to query. Override in derived classes to implement custom search logic.
    /// </summary>
    /// <param name="query">The base query</param>
    /// <param name="search">The search criteria</param>
    /// <returns>Filtered query</returns>
    protected virtual IQueryable<TModel> ApplySearchFilter(IQueryable<TModel> query, TSearch search)
    {
        // Default implementation returns unfiltered query
        // Override in derived controllers to implement search
        return query;
    }

    /// <summary>
    /// Get a specific entity by ID
    /// </summary>
    [HttpGet("{id}")]
    public virtual async Task<ActionResult<TDto>> GetById(Guid id)
    {
        var entity = await _dbSet.FindAsync(id);

        if (entity == null)
        {
            return NotFound(new { message = $"Entity with ID {id} not found" });
        }

        return Ok(MapToDto(entity));
    }

    /// <summary>
    /// Create a new entity
    /// </summary>
    [HttpPost]
    public virtual async Task<ActionResult<TDto>> Create([FromBody] TDto dto)
    {
        var entity = MapToEntity(dto);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        _dbSet.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, MapToDto(entity));
    }

    /// <summary>
    /// Update an existing entity
    /// </summary>
    [HttpPut("{id}")]
    public virtual async Task<ActionResult<TDto>> Update(Guid id, [FromBody] TDto dto)
    {
        var entity = await _dbSet.FindAsync(id);

        if (entity == null)
        {
            return NotFound(new { message = $"Entity with ID {id} not found" });
        }

        // Map DTO to existing entity
        entity = MapToEntity(dto, entity);
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        _context.Entry(entity).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await EntityExists(id))
            {
                return NotFound(new { message = $"Entity with ID {id} not found" });
            }
            throw;
        }

        return Ok(MapToDto(entity));
    }

    /// <summary>
    /// Delete an entity
    /// </summary>
    [HttpDelete("{id}")]
    public virtual async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _dbSet.FindAsync(id);

        if (entity == null)
        {
            return NotFound(new { message = $"Entity with ID {id} not found" });
        }

        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Check if an entity exists
    /// </summary>
    protected virtual async Task<bool> EntityExists(Guid id)
    {
        return await _dbSet.AnyAsync(e => e.Id == id);
    }
}
