using System.Net.Http.Json;
using blazor_wasm.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace blazor_wasm.Services;

public class AttachmentService : CrudService<AttachmentDto, AttachmentSearchDto>
{
    private readonly HttpClient _http;

    public AttachmentService(HttpClient httpClient)
        : base(httpClient, "api/Attachment")
    {
        _http = httpClient;
    }

    public async Task<AttachmentDto?> UploadAsync(IBrowserFile file, Guid parentId, ParentType parentType, long maxSizeBytes = 50 * 1024 * 1024)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var stream = file.OpenReadStream(maxSizeBytes);
            content.Add(new StreamContent(stream), "file", file.Name);
            content.Add(new StringContent(parentId.ToString()), "parentId");
            content.Add(new StringContent(((int)parentType).ToString()), "parentType");

            var response = await _http.PostAsync("api/Attachment/upload", content);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<AttachmentDto>();

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading attachment: {ex.Message}");
            return null;
        }
    }

    public async Task<byte[]?> DownloadAsync(Guid id)
    {
        try
        {
            var response = await _http.GetAsync($"api/Attachment/{id}/download");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsByteArrayAsync();

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading attachment: {ex.Message}");
            return null;
        }
    }
}
