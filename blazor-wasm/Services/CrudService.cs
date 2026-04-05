using System.Net.Http.Json;
using blazor_wasm.Models;

namespace blazor_wasm.Services;

/// <summary>
/// Generic CRUD service for entities that use ModelDtoControllerBase pattern
/// Provides standard Create, Read, Update, Delete, and Search operations
/// </summary>
/// <typeparam name="TDto">The DTO type</typeparam>
/// <typeparam name="TSearchDto">The search DTO type</typeparam>
public class CrudService<TDto, TSearchDto> where TSearchDto : SearchDto, new()
{
    private readonly HttpClient _httpClient;
    private readonly string _controllerRoute;

    /// <summary>
    /// Initialize the CRUD service
    /// </summary>
    /// <param name="httpClient">HTTP client for API calls</param>
    /// <param name="controllerRoute">Controller route (e.g., "api/Event")</param>
    public CrudService(HttpClient httpClient, string controllerRoute)
    {
        _httpClient = httpClient;
        _controllerRoute = controllerRoute.TrimEnd('/');
    }

    /// <summary>
    /// Search entities with pagination
    /// </summary>
    public async Task<PaginationDto<TDto>?> SearchAsync(TSearchDto searchDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_controllerRoute}/search", searchDto);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PaginationDto<TDto>>();
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching {typeof(TDto).Name}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get entity by ID
    /// </summary>
    public async Task<TDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_controllerRoute}/{id}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TDto>();
            }

            return default;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting {typeof(TDto).Name} by ID: {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// Create a new entity
    /// </summary>
    public async Task<TDto?> CreateAsync(TDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(_controllerRoute, dto);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TDto>();
            }

            return default;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating {typeof(TDto).Name}: {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// Update an existing entity
    /// </summary>
    public async Task<TDto?> UpdateAsync(Guid id, TDto dto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{_controllerRoute}/{id}", dto);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TDto>();
            }

            return default;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating {typeof(TDto).Name}: {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// Delete an entity by ID
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_controllerRoute}/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting {typeof(TDto).Name}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get error message from response
    /// </summary>
    public async Task<string?> GetErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            return content;
        }
        catch
        {
            return null;
        }
    }
}
