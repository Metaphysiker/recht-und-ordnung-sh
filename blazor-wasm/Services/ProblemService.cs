using blazor_wasm.Models;

namespace blazor_wasm.Services;

/// <summary>
/// Service for managing Problem entities
/// </summary>
public class ProblemService : CrudService<ProblemDto, ProblemSearchDto>
{
    public ProblemService(HttpClient httpClient)
        : base(httpClient, "api/Problem")
    {
    }
}
