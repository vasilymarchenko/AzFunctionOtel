using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FunctionApp1;

public class SystemMetricsFunction
{
    private readonly ILogger<SystemMetricsFunction> _logger;
    private static readonly ActivitySource ActivitySource = new("FunctionApp1");
    private static readonly Meter Meter = new("FunctionApp1");
    
    // Custom metrics that work with OpenTelemetry
    private static readonly Counter<int> SystemMetricsRequestCounter = Meter.CreateCounter<int>("system_metrics_requests_total", "Total number of system metrics requests");
    private static readonly Histogram<long> ManagedMemoryHistogram = Meter.CreateHistogram<long>("dotnet_managed_memory_bytes", "Managed memory usage in bytes");
    private static readonly Histogram<long> TotalMemoryHistogram = Meter.CreateHistogram<long>("dotnet_total_memory_bytes", "Total memory usage in bytes");
    private static readonly Histogram<double> CpuUsageHistogram = Meter.CreateHistogram<double>("dotnet_cpu_usage_percent", "CPU usage percentage");
    private static readonly Histogram<int> ThreadCountHistogram = Meter.CreateHistogram<int>("dotnet_thread_count", "Current thread count");
    private static readonly Counter<long> GcCollectionsCounter = Meter.CreateCounter<long>("dotnet_gc_collections_total", "Total garbage collections");

    private static DateTime _lastCpuTime = DateTime.UtcNow;
    private static TimeSpan _lastProcessorTime = Process.GetCurrentProcess().TotalProcessorTime;

    public SystemMetricsFunction(ILogger<SystemMetricsFunction> logger)
    {
        _logger = logger;
    }

    [Function("SystemMetrics")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        using var activity = ActivitySource.StartActivity("SystemMetricsFunction.Run");
        
        try
        {
            _logger.LogInformation("System metrics endpoint requested.");
            
            activity?.SetTag("function.name", "SystemMetrics");
            
            SystemMetricsRequestCounter.Add(1, 
                new KeyValuePair<string, object?>("function", "SystemMetrics"));
            
            var process = Process.GetCurrentProcess();
            
            // Collect and record system metrics
            var managedMemory = GC.GetTotalMemory(false);
            var totalMemory = process.WorkingSet64;
            var threadCount = process.Threads.Count;
            var cpuUsage = CalculateCpuUsage(process);
            
            // GC Statistics
            var gen0Collections = GC.CollectionCount(0);
            var gen1Collections = GC.CollectionCount(1);
            var gen2Collections = GC.CollectionCount(2);
            
            // Record metrics using histograms for current values
            ManagedMemoryHistogram.Record(managedMemory);
            TotalMemoryHistogram.Record(totalMemory);
            ThreadCountHistogram.Record(threadCount);
            CpuUsageHistogram.Record(cpuUsage);
            
            // Record GC collections
            GcCollectionsCounter.Add(gen0Collections, 
                new KeyValuePair<string, object?>("generation", "0"));
            GcCollectionsCounter.Add(gen1Collections, 
                new KeyValuePair<string, object?>("generation", "1"));
            GcCollectionsCounter.Add(gen2Collections, 
                new KeyValuePair<string, object?>("generation", "2"));
            
            var systemMetrics = new
            {
                message = "System metrics collected",
                timestamp = DateTime.UtcNow,
                service = "FunctionApp1",
                metrics = new
                {
                    managed_memory_bytes = managedMemory,
                    total_memory_bytes = totalMemory,
                    cpu_usage_percent = cpuUsage,
                    thread_count = threadCount,
                    garbage_collection = new
                    {
                        gen0_collections = gen0Collections,
                        gen1_collections = gen1Collections,
                        gen2_collections = gen2Collections
                    },
                    process_info = new
                    {
                        process_id = process.Id,
                        start_time = process.StartTime,
                        uptime_ms = Environment.TickCount64,
                        processor_count = Environment.ProcessorCount
                    }
                }
            };
            
            return new OkObjectResult(systemMetrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "System metrics endpoint failed");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new StatusCodeResult(500);
        }
    }
    
    private static double CalculateCpuUsage(Process process)
    {
        try
        {
            var currentTime = DateTime.UtcNow;
            var currentProcessorTime = process.TotalProcessorTime;
            
            var timeDelta = currentTime - _lastCpuTime;
            var processorTimeDelta = currentProcessorTime - _lastProcessorTime;
            
            var cpuUsage = 0.0;
            if (timeDelta.TotalMilliseconds > 0)
            {
                cpuUsage = (processorTimeDelta.TotalMilliseconds / timeDelta.TotalMilliseconds) * 100.0 / Environment.ProcessorCount;
            }
            
            _lastCpuTime = currentTime;
            _lastProcessorTime = currentProcessorTime;
            
            return Math.Round(cpuUsage, 2);
        }
        catch
        {
            return 0.0;
        }
    }
}
