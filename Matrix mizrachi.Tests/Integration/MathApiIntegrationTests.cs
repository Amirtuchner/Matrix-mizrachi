using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Matrix_mizrachi.Models;
using Matrix_mizrachi.Services;
using Moq;
using Xunit;

namespace Matrix_mizrachi.Tests.Integration;

/// <summary>
/// Integration tests using TestServer (WebApplicationFactory).
/// IMockoonService and IKafkaProducerService are replaced with Moq stubs.
/// The memory cache is shared across tests in this class — each test uses
/// unique operand values to avoid cross-test cache collisions.
/// </summary>
public class MathApiIntegrationTests : IClassFixture<MathApiFactory>, IAsyncLifetime
{
    private readonly MathApiFactory _factory;
    private readonly HttpClient _client;

    public MathApiIntegrationTests(MathApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // Reset mocks before each test so invocation counts start at zero.
    public Task InitializeAsync()
    {
        _factory.MockoonMock.Reset();
        _factory.KafkaMock.Reset();
        _factory.KafkaMock
            .Setup(k => k.PublishAsync(It.IsAny<MathOperationEvent>()))
            .Returns(Task.CompletedTask);
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ---------------------------------------------------------------------------
    // Helper
    // ---------------------------------------------------------------------------

    private async Task<string> GetTokenAsync()
    {
        var response = await _client.PostAsync("/api/auth/token", null);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("token").GetString()!;
    }

    private HttpRequestMessage BuildMathRequest(
        string token, string operation, double x, double y)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/math")
        {
            Content = JsonContent.Create(new MathRequest { Operation = operation, X = x, Y = y })
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Add("X-ArithmeticOp-ID", operation);
        return req;
    }

    // ---------------------------------------------------------------------------
    // Auth tests
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task PostToken_Returns200_WithJwt()
    {
        var response = await _client.PostAsync("/api/auth/token", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = json.GetProperty("token").GetString();
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        // Quick sanity: a JWT has three dot-separated parts
        Assert.Equal(3, token.Split('.').Length);
    }

    [Fact]
    public async Task PostMath_WithoutToken_Returns401()
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/math")
        {
            Content = JsonContent.Create(new MathRequest { Operation = "add", X = 1, Y = 1 })
        };
        req.Headers.Add("X-ArithmeticOp-ID", "add");

        var response = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------------------------------------------------------------------------
    // Cache-miss: Mockoon stub + Kafka publish
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task PostMath_ValidAdd_ReturnsCacheMissResult()
    {
        _factory.MockoonMock
            .Setup(m => m.GetOperationDescriptionAsync("add"))
            .ReturnsAsync("Performs add arithmetic");

        var token = await GetTokenAsync();
        var response = await _client.SendAsync(BuildMathRequest(token, "add", 10, 5));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MathResponse>();
        Assert.NotNull(result);
        Assert.Equal(15, result.Result);
        Assert.Equal("Performs add arithmetic", result.Description);
        Assert.False(result.FromCache);

        _factory.MockoonMock.Verify(m => m.GetOperationDescriptionAsync("add"), Times.Once);
        _factory.KafkaMock.Verify(k => k.PublishAsync(It.IsAny<MathOperationEvent>()), Times.Once);
    }

    // ---------------------------------------------------------------------------
    // Cache-hit: second identical request served from cache
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task PostMath_SecondIdenticalRequest_ReturnsCacheHit()
    {
        _factory.MockoonMock
            .Setup(m => m.GetOperationDescriptionAsync("subtract"))
            .ReturnsAsync("Performs subtract arithmetic");

        var token = await GetTokenAsync();

        // First call — cache miss
        var first = await _client.SendAsync(BuildMathRequest(token, "subtract", 20, 8));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstResult = await first.Content.ReadFromJsonAsync<MathResponse>();
        Assert.False(firstResult!.FromCache);
        Assert.Equal(12, firstResult.Result);

        // Second call with same inputs — cache hit
        var second = await _client.SendAsync(BuildMathRequest(token, "subtract", 20, 8));
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondResult = await second.Content.ReadFromJsonAsync<MathResponse>();
        Assert.True(secondResult!.FromCache);
        Assert.Equal(12, secondResult.Result);

        // Mockoon and Kafka called only on the first (cache-miss) request
        _factory.MockoonMock.Verify(m => m.GetOperationDescriptionAsync("subtract"), Times.Once);
        _factory.KafkaMock.Verify(k => k.PublishAsync(It.IsAny<MathOperationEvent>()), Times.Once);
    }

    // ---------------------------------------------------------------------------
    // Error handling
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task PostMath_DivideByZero_Returns400WithErrorMessage()
    {
        _factory.MockoonMock
            .Setup(m => m.GetOperationDescriptionAsync("divide"))
            .ReturnsAsync("Performs divide arithmetic");

        var token = await GetTokenAsync();
        var response = await _client.SendAsync(BuildMathRequest(token, "divide", 10, 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var error = json.GetProperty("error").GetString();
        Assert.Contains("zero", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostMath_UnknownOperation_Returns400WithErrorMessage()
    {
        _factory.MockoonMock
            .Setup(m => m.GetOperationDescriptionAsync("modulo"))
            .ReturnsAsync(string.Empty);

        var token = await GetTokenAsync();
        var response = await _client.SendAsync(BuildMathRequest(token, "modulo", 10, 3));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var error = json.GetProperty("error").GetString();
        Assert.Contains("modulo", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostMath_MissingArithmeticOpIdHeader_Returns400()
    {
        var token = await GetTokenAsync();
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/math")
        {
            Content = JsonContent.Create(new MathRequest { Operation = "add", X = 1, Y = 2 })
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        // No X-ArithmeticOp-ID header

        var response = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------------------------------------------------------------------------
    // Kafka event payload
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task PostMath_OnCacheMiss_PublishesKafkaEventWithCorrectValues()
    {
        _factory.MockoonMock
            .Setup(m => m.GetOperationDescriptionAsync("multiply"))
            .ReturnsAsync("Performs multiply arithmetic");

        MathOperationEvent? captured = null;
        _factory.KafkaMock
            .Setup(k => k.PublishAsync(It.IsAny<MathOperationEvent>()))
            .Callback<MathOperationEvent>(e => captured = e)
            .Returns(Task.CompletedTask);

        var token = await GetTokenAsync();
        await _client.SendAsync(BuildMathRequest(token, "multiply", 3, 7));

        Assert.NotNull(captured);
        Assert.Equal("multiply", captured!.Operation);
        Assert.Equal(3, captured.X);
        Assert.Equal(7, captured.Y);
        Assert.Equal(21, captured.Result);
        Assert.NotEmpty(captured.RequestId);
    }
}
