using System.Net.Http.Json;
using UserBrowser.Models;

namespace UserBrowser.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _http;

    // HttpClient is constructor-injected (registered in MauiProgram.cs).
    public ApiService(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        // Relative to HttpClient.BaseAddress.
        var response = await _http.GetAsync("users", cancellationToken);
        response.EnsureSuccessStatusCode();   // throws HttpRequestException on non-2xx

        var users = await response.Content.ReadFromJsonAsync<List<User>>(cancellationToken);
        return users ?? new List<User>();
    }
}
