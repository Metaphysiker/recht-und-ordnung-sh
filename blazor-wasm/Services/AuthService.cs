using System.Net.Http.Json;
using blazor_wasm.Models;
using Microsoft.JSInterop;

namespace blazor_wasm.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private const string TokenKey = "authToken";

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto model)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/register", model);

        if (response.IsSuccessStatusCode)
        {
            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            if (authResponse != null)
            {
                await SetTokenAsync(authResponse.Token);
            }
            return authResponse;
        }

        return null;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto model)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", model);

        if (response.IsSuccessStatusCode)
        {
            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            if (authResponse != null)
            {
                await SetTokenAsync(authResponse.Token);
            }
            return authResponse;
        }

        return null;
    }

    public async Task LogoutAsync()
    {
        await RemoveTokenAsync();
    }

    public async Task<bool> RefreshTokenAsync()
    {
        var token = await GetTokenAsync();
        
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var response = await _httpClient.PostAsync("api/auth/refresh", null);

            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
                if (authResponse != null)
                {
                    await SetTokenAsync(authResponse.Token);
                    return true;
                }
            }
        }
        catch
        {
            // If refresh fails, logout
            await LogoutAsync();
        }

        return false;
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
    }

    private async Task SetTokenAsync(string token)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
    }

    private async Task RemoveTokenAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
    }
}
