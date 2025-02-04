using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Indice.Functions.Builder.HealthCheck;

/// <summary>
/// Provides health check functions for the application.
/// </summary>
public class HealthCheckFunctions
{
    private readonly ILogger<HealthCheckFunctions> _logger;
    private readonly HealthCheckService _healthCheck;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthCheckFunctions"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="healthCheck">The health check service instance.</param>
    public HealthCheckFunctions(ILogger<HealthCheckFunctions> logger, HealthCheckService healthCheck)
    {
        _healthCheck = healthCheck;
        _logger = logger;
    }

    /// <summary>
    /// Handles the health check request.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <returns>The health check status.</returns>
    [Function("HealthCheck")]
    public async Task<IActionResult> HealthCheck([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "health")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        var healthStatus = await _healthCheck.CheckHealthAsync();
        return new OkObjectResult(Enum.GetName(typeof(HealthStatus), healthStatus.Status));
    }

    /// <summary>
    /// Handles the detailed health check request.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <returns>The detailed health check status.</returns>
    [Function("HealthCheckDetails")]
    public async Task<IActionResult> HealthCheckDetails([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "health/details")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        var healthStatus = await _healthCheck.CheckHealthAsync();
        return new OkObjectResult(healthStatus);
    }

    /// <summary>
    /// Handles the liveness check request.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <returns>The liveness check status.</returns>
    [Function("Alive")]
    public async Task<IActionResult> Alive([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "alive")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        var healthStatus = await _healthCheck.CheckHealthAsync(r => r.Tags.Contains("live"));
        return new OkObjectResult(Enum.GetName(typeof(HealthStatus), healthStatus.Status));
    }
}
