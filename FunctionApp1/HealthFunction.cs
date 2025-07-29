using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FunctionApp1;

public class HealthFunction
{
    private readonly ILogger<HealthFunction> _logger;
    private static readonly ActivitySource ActivitySource = new("FunctionApp1");
    private static readonly Meter Meter = new("FunctionApp1");
    private static readonly Counter<int> HealthCheckCounter = Meter.CreateCounter<int>("health_checks_total", "Total number of health check requests");

    public HealthFunction(ILogger<HealthFunction> logger)
    {
        _logger = logger;
    }

    [Function("Health")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        using var activity = ActivitySource.StartActivity("HealthFunction.Run");
        
        try
        {
            _logger.LogInformation("Health check requested.");
            
            activity?.SetTag("function.name", "Health");
            activity?.SetTag("health.status", "healthy");
            
            HealthCheckCounter.Add(1, 
                new KeyValuePair<string, object?>("function", "Health"),
                new KeyValuePair<string, object?>("status", "healthy"));
            
            var healthResponse = new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "FunctionApp1",
                version = "1.0.0",
                uptime = Environment.TickCount64
            };
            
            return new OkObjectResult(healthResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            activity?.SetTag("health.status", "unhealthy");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            HealthCheckCounter.Add(1, 
                new KeyValuePair<string, object?>("function", "Health"),
                new KeyValuePair<string, object?>("status", "unhealthy"));
            
            return new StatusCodeResult(503);
        }
    }
}
