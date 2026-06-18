using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Matrix_mizrachi.Models;
using Matrix_mizrachi.Services;

namespace Matrix_mizrachi.Controllers;

/// <summary>
/// Performs arithmetic operations with caching and Kafka integration.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MathController : ControllerBase
{
    private readonly IMathService _mathService;
    private readonly IMockoonService _mockoonService;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly IMemoryCache _cache;
    private readonly ILogger<MathController> _logger;

    public MathController(
        IMathService mathService,
        IMockoonService mockoonService,
        IKafkaProducerService kafkaProducer,
        IMemoryCache cache,
        ILogger<MathController> logger)
    {
        _mathService = mathService;
        _mockoonService = mockoonService;
        _kafkaProducer = kafkaProducer;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Performs an arithmetic operation on two numbers.
    /// Returns cached result if available (30-second TTL).
    /// On cache miss, calls Mockoon for operation description and publishes a Kafka event.
    /// </summary>
    /// <param name="request">Request body containing operation, x, y.</param>
    /// <param name="arithmeticOpId">Required header X-ArithmeticOp-ID specifying the operation type.</param>
    [HttpPost]
    [ProducesResponseType(typeof(MathResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Calculate(
        [FromBody] MathRequest request,
        [FromHeader(Name = "X-ArithmeticOp-ID")] string arithmeticOpId)
    {
        if (string.IsNullOrWhiteSpace(arithmeticOpId))
            return BadRequest(new { error = "X-ArithmeticOp-ID header is required." });

        var operation = arithmeticOpId.Trim();
        var cacheKey = $"{operation}:{request.X}:{request.Y}";

        _logger.LogInformation("Math request: operation={Operation}, x={X}, y={Y}", operation, request.X, request.Y);

        // Cache hit
        if (_cache.TryGetValue(cacheKey, out CachedMathResult? cached))
        {
            _logger.LogInformation("Math response (cache hit): operation={Operation}, result={Result}, description={Description}",
                operation, cached!.Result, cached.Description);
            return Ok(new MathResponse
            {
                Result = cached!.Result,
                Description = cached.Description,
                FromCache = true
            });
        }

        // Cache miss - call Mockoon for description
        var description = await _mockoonService.GetOperationDescriptionAsync(operation);

        // Perform calculation (throws on divide-by-zero or unknown op, caught by middleware)
        var result = _mathService.Calculate(operation, request.X, request.Y);

        // Store in cache for 30 seconds
        _cache.Set(cacheKey, new CachedMathResult(result, description), TimeSpan.FromSeconds(30));

        // Publish Kafka event
        await _kafkaProducer.PublishAsync(new MathOperationEvent
        {
            Operation = operation,
            X = request.X,
            Y = request.Y,
            Result = result
        });

        _logger.LogInformation("Math response (cache miss): operation={Operation}, result={Result}, description={Description}",
            operation, result, description);
        return Ok(new MathResponse
        {
            Result = result,
            Description = description,
            FromCache = false
        });
    }

    private record CachedMathResult(double Result, string Description);
}
