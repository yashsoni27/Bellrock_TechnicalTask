using System.Net.Http.Json;

namespace DotNetInterview.Web.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    public ApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("API");
    }

    public async Task<T> GetAsync<T>(string endpoint)
    {
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        var response = await _httpClient.PostAsJsonAsync(endpoint, data);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    public async Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(endpoint, data);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"PUT request failed: {error}");
            }
            
            return await response.Content.ReadFromJsonAsync<TResponse>();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task DeleteAsync(string endpoint)
    {
        var response = await _httpClient.DeleteAsync(endpoint);
        response.EnsureSuccessStatusCode();
    }
} 