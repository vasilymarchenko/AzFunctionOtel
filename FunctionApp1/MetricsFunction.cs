using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FunctionApp1;

public class MetricsFunction
{
    private readonly ILogger<MetricsFunction> _logger;
    private static readonly ActivitySource ActivitySource = new("FunctionApp1");
    private static readonly Meter Meter = new("FunctionApp1");
    private static readonly Counter<int> MetricsRequestCounter = Meter.CreateCounter<int>("metrics_requests_total", "Total number of metrics endpoint requests");
    private static readonly UpDownCounter<long> SystemMemoryUsage = Meter.CreateUpDownCounter<long>("system_memory_usage_bytes", "Current system memory usage in bytes");
    private static readonly Counter<int> ProcessInfoCounter = Meter.CreateCounter<int>("process_info_requests", "Process information requests");

    public MetricsFunction(ILogger<MetricsFunction> logger)
    {
        _logger = logger;
    }

    [Function("Metrics")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        using var activity = ActivitySource.StartActivity("MetricsFunction.Run");
        
        try
        {
            _logger.LogInformation("Metrics endpoint requested.");
            
            activity?.SetTag("function.name", "Metrics");
            
            MetricsRequestCounter.Add(1, 
                new KeyValuePair<string, object?>("function", "Metrics"));
            
            // Record some system metrics
            var process = Process.GetCurrentProcess();
            SystemMemoryUsage.Add(process.WorkingSet64);
            ProcessInfoCounter.Add(1, 
                new KeyValuePair<string, object?>("function", "Metrics"),
                new KeyValuePair<string, object?>("process_id", process.Id));
            
            var metricsResponse = new
            {
                message = "Metrics are being collected via OpenTelemetry",
                timestamp = DateTime.UtcNow,
                service = "FunctionApp1",
                memory_usage_bytes = process.WorkingSet64,
                process_start_time = process.StartTime,
                uptime_ms = Environment.TickCount64
            };
            
            return new OkObjectResult(metricsResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Metrics endpoint failed");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new StatusCodeResult(500);
        }
    }
}
