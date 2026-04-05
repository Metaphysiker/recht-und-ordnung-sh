using blazor_wasm.Models;

namespace blazor_wasm.Services;

/// <summary>
/// Service for managing Event entities
/// </summary>
public class EventService : CrudService<EventDto, EventSearchDto>
{
    public EventService(HttpClient httpClient)
        : base(httpClient, "api/Event")
    {
    }
}
