using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using blazor_wasm;
using blazor_wasm.Services;
using blazor_wasm.Handlers;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Get API URL from configuration
var apiUrl = builder.Configuration["ApiUrl"] ?? "http://localhost:5001";

// Register AuthService
builder.Services.AddScoped<AuthService>();

// Register CRUD services
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<ProblemService>();

// Register custom AuthStateProvider
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<CustomAuthStateProvider>());

// Register Authorization
builder.Services.AddAuthorizationCore();

// Register HttpClient with Authorization handler
builder.Services.AddTransient<AuthorizationMessageHandler>();
builder.Services.AddScoped(sp =>
{
    var authHandler = sp.GetRequiredService<AuthorizationMessageHandler>();
    authHandler.InnerHandler = new HttpClientHandler();

    var httpClient = new HttpClient(authHandler)
    {
        BaseAddress = new Uri(apiUrl)
    };

    return httpClient;
});

builder.Services.AddMudServices();

var host = builder.Build();

// Notify authentication state on app startup
var authStateProvider = host.Services.GetRequiredService<CustomAuthStateProvider>();
authStateProvider.NotifyAuthenticationStateChanged();

await host.RunAsync();
