using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FinPulse.Tests.Helpers.Extensions;

/// <summary>
/// Extension methods for HttpClient to simplify integration testing.
/// </summary>
public static class HttpClientExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Adds a JWT bearer token to the HTTP client's authorization header.
    /// </summary>
    public static void AddAuthToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Sends a POST request with JSON content.
    /// </summary>
    public static async Task<HttpResponseMessage> PostAsJsonAsync<T>(
        this HttpClient client, string url, T data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PostAsync(url, content);
    }

    /// <summary>
    /// Sends a PUT request with JSON content.
    /// </summary>
    public static async Task<HttpResponseMessage> PutAsJsonAsync<T>(
        this HttpClient client, string url, T data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PutAsync(url, content);
    }

    /// <summary>
    /// Reads and deserializes JSON response content.
    /// </summary>
    public static async Task<T?> ReadAsJsonAsync<T>(this HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(json))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }
}
