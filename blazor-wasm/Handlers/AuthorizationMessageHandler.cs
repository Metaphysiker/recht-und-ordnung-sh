using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using blazor_wasm.Services;

namespace blazor_wasm.Handlers;

public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly IJSRuntime _jsRuntime;
    private readonly CustomAuthStateProvider _authStateProvider;
    private readonly AuthService _authService;
    private readonly JwtSecurityTokenHandler _jwtHandler = new();
    private const string TokenKey = "authToken";
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);

    public AuthorizationMessageHandler(
        IJSRuntime jsRuntime,
        CustomAuthStateProvider authStateProvider,
        AuthService authService)
    {
        _jsRuntime = jsRuntime;
        _authStateProvider = authStateProvider;
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);

        if (!string.IsNullOrWhiteSpace(token))
        {
            // Check if token is about to expire (within 1 day) and refresh it
            if (ShouldRefreshToken(token))
            {
                await RefreshTokenAsync();
                // Get the new token after refresh
                token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
            }

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Handle 401 Unauthorized - token expired or invalid
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Clear the token from localStorage
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
            // Notify authentication state has changed
            _authStateProvider.NotifyAuthenticationStateChanged();
        }

        return response;
    }

    private bool ShouldRefreshToken(string token)
    {
        try
        {
            var jwtToken = _jwtHandler.ReadJwtToken(token);
            // Refresh if token expires within 1 day
            return jwtToken.ValidTo < DateTime.UtcNow.AddDays(1);
        }
        catch
        {
            return false;
        }
    }

    private async Task RefreshTokenAsync()
    {
        // Use semaphore to prevent multiple simultaneous refresh attempts
        await _refreshLock.WaitAsync();
        try
        {
            await _authService.RefreshTokenAsync();
            _authStateProvider.NotifyAuthenticationStateChanged();
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
