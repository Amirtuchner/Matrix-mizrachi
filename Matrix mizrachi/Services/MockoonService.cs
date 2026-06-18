using System.Text.Json;

namespace Matrix_mizrachi.Services;

/// <summary>
/// Retrieves operation metadata from the Mockoon stub server.
/// </summary>
public class MockoonService : IMockoonService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MockoonService> _logger;

    public MockoonService(HttpClient httpClient, ILogger<MockoonService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string> GetOperationDescriptionAsync(string operation)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/meta/{operation}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Mockoon response for operation '{Operation}': {Content}", operation, content);

                // Try to extract description field from JSON
                try
                {
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.TryGetProperty("description", out var desc))
                        return desc.GetString() ?? content;
                }
                catch
                {
                    // Not JSON or no description field - return raw content
                }

                return content;
            }

            _logger.LogWarning("Mockoon returned {StatusCode} for operation '{Operation}'", response.StatusCode, operation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Mockoon for operation '{Operation}'", operation);
        }

        return string.Empty;
    }
}
