using System.Text;
using System.Text.Json;

namespace GymSystem.Mvc.Helpers;

/// <summary>
/// API çağrıları için helper class - Tekrar eden kodu azaltır
/// </summary>
public class ApiHelper {
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiHelper> _logger;

    public ApiHelper(IHttpClientFactory httpClientFactory, ILogger<ApiHelper> logger) {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// GET request - Tek nesne döner
    /// </summary>
    public async Task<T?> GetAsync<T>(string endpoint) where T : class {
        try {
            var client = _httpClientFactory.CreateClient("GymApi");
            var response = await client.GetAsync(endpoint);

            if (response.IsSuccessStatusCode) {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogWarning("API GET request failed. Endpoint: {Endpoint}, Status: {Status}",
                endpoint, response.StatusCode);
            return null;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "API GET request exception. Endpoint: {Endpoint}", endpoint);
            return null;
        }
    }

    /// <summary>
    /// GET request - Liste döner
    /// </summary>
    public async Task<List<T>> GetListAsync<T>(string endpoint) where T : class {
        try {
            var client = _httpClientFactory.CreateClient("GymApi");
            var response = await client.GetAsync(endpoint);

            if (response.IsSuccessStatusCode) {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<T>>(content, new JsonSerializerOptions {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<T>();
            }

            _logger.LogWarning("API GET list request failed. Endpoint: {Endpoint}, Status: {Status}",
                endpoint, response.StatusCode);
            return new List<T>();
        }
        catch (Exception ex) {
            _logger.LogError(ex, "API GET list request exception. Endpoint: {Endpoint}", endpoint);
            return new List<T>();
        }
    }

    /// <summary>
    /// POST request
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> PostAsync<T>(string endpoint, T data) {
        try {
            var client = _httpClientFactory.CreateClient("GymApi");
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(endpoint, content);

            if (response.IsSuccessStatusCode) {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("API POST request failed. Endpoint: {Endpoint}, Status: {Status}, Error: {Error}",
                endpoint, response.StatusCode, errorContent);

            return (false, $"Status: {response.StatusCode}");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "API POST request exception. Endpoint: {Endpoint}", endpoint);
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// POST request - Response döner
    /// </summary>
    public async Task<(bool Success, TResponse? Data, string? ErrorMessage)> PostWithResponseAsync<TRequest, TResponse>(string endpoint, TRequest data)
        where TResponse : class {
        try {
            var client = _httpClientFactory.CreateClient("GymApi");
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(endpoint, content);

            if (response.IsSuccessStatusCode) {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TResponse>(responseContent, new JsonSerializerOptions {
                    PropertyNameCaseInsensitive = true
                });
                return (true, result, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("API POST request failed. Endpoint: {Endpoint}, Status: {Status}, Error: {Error}",
                endpoint, response.StatusCode, errorContent);

            return (false, null, $"Status: {response.StatusCode}");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "API POST request exception. Endpoint: {Endpoint}", endpoint);
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// PUT request
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> PutAsync<T>(string endpoint, T data) {
        try {
            var client = _httpClientFactory.CreateClient("GymApi");
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync(endpoint, content);

            if (response.IsSuccessStatusCode) {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("API PUT request failed. Endpoint: {Endpoint}, Status: {Status}, Error: {Error}",
                endpoint, response.StatusCode, errorContent);

            return (false, $"Status: {response.StatusCode}");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "API PUT request exception. Endpoint: {Endpoint}", endpoint);
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// DELETE request
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(string endpoint) {
        try {
            var client = _httpClientFactory.CreateClient("GymApi");
            var response = await client.DeleteAsync(endpoint);

            if (response.IsSuccessStatusCode) {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("API DELETE request failed. Endpoint: {Endpoint}, Status: {Status}, Error: {Error}",
                endpoint, response.StatusCode, errorContent);

            return (false, $"Status: {response.StatusCode}");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "API DELETE request exception. Endpoint: {Endpoint}", endpoint);
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Raw HttpResponseMessage döner (özel durumlar için)
    /// </summary>
    public async Task<HttpResponseMessage> GetRawAsync(string endpoint) {
        var client = _httpClientFactory.CreateClient("GymApi");
        return await client.GetAsync(endpoint);
    }
}