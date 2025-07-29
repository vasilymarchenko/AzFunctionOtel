using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FunctionApp1;

public class Function1
{
    private readonly ILogger<Function1> _logger;
    private static readonly ActivitySource ActivitySource = new("FunctionApp1");
    private static readonly Meter Meter = new("FunctionApp1");
    private static readonly Counter<int> RequestCounter = Meter.CreateCounter<int>("function_requests_total", "Total number of function requests");
    private static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>("function_request_duration_ms", "Duration of function requests in milliseconds");

    public Function1(ILogger<Function1> logger)
    {
        _logger = logger;
    }

    [Function("Function1")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        using var activity = ActivitySource.StartActivity("Function1.Run");
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            
            // Add tags to the activity
            activity?.SetTag("function.name", "Function1");
            activity?.SetTag("http.method", req.Method);
            activity?.SetTag("http.path", req.Path);
            
            // Increment request counter
            RequestCounter.Add(1, 
                new KeyValuePair<string, object?>("function", "Function1"),
                new KeyValuePair<string, object?>("method", req.Method));
            
            // Simulate some work
            Thread.Sleep(Random.Shared.Next(50, 200));
            
            var response = new { 
                message = "Welcome to Azure Functions!",
                timestamp = DateTime.UtcNow,
                requestId = Activity.Current?.Id ?? Guid.NewGuid().ToString()
            };
            
            activity?.SetTag("response.status", "200");
            
            return new OkObjectResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request");
            activity?.SetTag("response.status", "500");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            RequestCounter.Add(1, 
                new KeyValuePair<string, object?>("function", "Function1"),
                new KeyValuePair<string, object?>("method", req.Method),
                new KeyValuePair<string, object?>("status", "error"));
            
            return new StatusCodeResult(500);
        }
        finally
        {
            stopwatch.Stop();
            RequestDuration.Record(stopwatch.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("function", "Function1"),
                new KeyValuePair<string, object?>("method", req.Method));
        }
    }
}