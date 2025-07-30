# Azure Function with OpenTelemetry and .NET Runtime Metrics

This project demonstrates how to collect metrics, traces, and logs from an Azure Function using OpenTelemetry, including comprehensive .NET runtime metrics.

## Components

### Function App
- **Function1**: Main HTTP trigger function with custom metrics and tracing
- **HealthFunction**: Health check endpoint
- **MetricsFunction**: Basic metrics information endpoint
- **SystemMetricsFunction**: Enhanced .NET runtime and system metrics endpoint

### OpenTelemetry Stack
- **OTEL Collector**: Collects and processes telemetry data
- **Prometheus**: Metrics storage and querying
- **Tempo**: Distributed tracing backend
- **Loki**: Log aggregation
- **Grafana**: Visualization and dashboards

## Setup and Running

1. **Build and run the services:**
   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.override.yml -f docker-compose.otel.yml up --build
   ```

2. **Test the function endpoints:**
   ```bash
   # Main function
   curl http://localhost:7071/api/Function1
   
   # Health check
   curl http://localhost:7071/api/Health
   
   # Basic metrics info
   curl http://localhost:7071/api/Metrics
   
   # Comprehensive system metrics
   curl http://localhost:7071/api/SystemMetrics
   ```

3. **Test all functionality including .NET runtime metrics:**
   ```powershell
   # Run the comprehensive test script
   .\test-telemetry.ps1
   ```

4. **Access monitoring interfaces:**
   - **Grafana**: http://localhost:3000
   - **Prometheus**: http://localhost:9091
   - **OTEL Collector metrics**: http://localhost:8889/metrics
   - **OTEL Collector zpages**: http://localhost:55679

## .NET Runtime and System Metrics

This application collects metrics from multiple sources to provide comprehensive observability:

### 📊 OpenTelemetry Automatic Instrumentation (OOTB Metrics)

These metrics are collected automatically by OpenTelemetry instrumentation packages **without any custom code**. Simply adding the NuGet packages and enabling the instrumentation provides these metrics:

#### **From `OpenTelemetry.Instrumentation.Runtime` package:**
**Enabled by**: `.AddRuntimeInstrumentation()` in Program.cs
- `process_runtime_dotnet_gc_collections_total`: Garbage collection counts by generation
- `process_runtime_dotnet_gc_objects_size_bytes`: Size of objects in GC heap by generation
- `process_runtime_dotnet_gc_allocations_size_bytes`: Bytes allocated on GC heap
- `process_runtime_dotnet_jit_compilation_time_seconds`: Time spent in JIT compilation
- `process_runtime_dotnet_thread_pool_threads_count`: Thread pool statistics (active, idle, limit)
- `process_runtime_dotnet_monitor_lock_contention_total`: Lock contention events
- `process_runtime_dotnet_time_in_gc_ratio`: Percentage of time spent in garbage collection

#### **From `OpenTelemetry.Instrumentation.Process` package:**
**Enabled by**: `.AddProcessInstrumentation()` in Program.cs
- `process_cpu_seconds_total`: Total CPU time consumed by the process
- `process_resident_memory_bytes`: Resident memory usage (RSS)
- `process_virtual_memory_bytes`: Virtual memory usage
- `process_working_set_bytes`: Working set memory
- `process_private_bytes`: Private memory bytes

#### **From `OpenTelemetry.Instrumentation.AspNetCore` package:**
**Enabled by**: `.AddAspNetCoreInstrumentation()` in Program.cs
- `http_server_request_duration`: HTTP request duration histogram
- `http_server_active_requests`: Number of active HTTP requests

#### **From `OpenTelemetry.Instrumentation.Http` package:**
**Enabled by**: `.AddHttpClientInstrumentation()` in Program.cs
- `http_client_request_duration`: HTTP client request duration
- `http_client_active_requests`: Number of active HTTP client requests

### 🔧 Custom Application Metrics (Manual Instrumentation)

These metrics are explicitly defined in the application code using OpenTelemetry.Metrics API:

#### **Function-level metrics:**
- `function_requests_total`: Counter of total function requests (Function1.cs)
- `function_request_duration_ms`: Histogram of request durations (Function1.cs)
- `health_checks_total`: Counter of health check requests (HealthFunction.cs)
- `metrics_requests_total`: Counter of metrics endpoint requests (MetricsFunction.cs)
- `system_metrics_requests_total`: Counter of system metrics requests (SystemMetricsFunction.cs)

#### **Custom system metrics collected via .NET APIs:**
- `dotnet_managed_memory_bytes`: Current managed memory usage via `GC.GetTotalMemory()`
- `dotnet_total_memory_bytes`: Total memory usage via `Process.WorkingSet64`
- `dotnet_cpu_usage_percent`: CPU usage percentage (calculated)
- `dotnet_thread_count`: Current thread count via `Process.Threads.Count`
- `dotnet_gc_collections_total`: GC collections by generation via `GC.CollectionCount()`

### 📈 Metrics Collection Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   .NET Runtime  │───▶│ OpenTelemetry    │───▶│ OTEL Collector  │
│   (.NET APIs)   │    │ Instrumentation  │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                                         │
┌─────────────────┐    ┌──────────────────┐             │
│ Custom Metrics  │───▶│ Manual Meters &  │─────────────┘
│ (Application)   │    │ Instruments      │             │
└─────────────────┘    └──────────────────┘             ▼
                                                ┌─────────────────┐
                                                │   Prometheus    │
                                                │    & Grafana    │
                                                └─────────────────┘
```

### 🔧 Configuration in Program.cs

The metrics collection is configured in `Program.cs` with the following setup:

```csharp
.WithMetrics(metrics => metrics
    .AddMeter(serviceName)                    // Enable custom app metrics
    .AddMeter("System.Runtime")               // Enable .NET runtime metrics  
    .AddAspNetCoreInstrumentation()           // Enable HTTP server metrics
    .AddHttpClientInstrumentation()           // Enable HTTP client metrics
    .AddRuntimeInstrumentation()              // Enable .NET runtime metrics
    .AddProcessInstrumentation()              // Enable process-level metrics
    .AddOtlpExporter(...)                     // Export to OTEL Collector
    .AddPrometheusExporter());                // Direct Prometheus export
```

### 🚀 Minimal Configuration for OOTB Metrics Only

If you only want the out-of-the-box metrics without custom metrics, you can use this minimal configuration:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddRuntimeInstrumentation()          // .NET runtime metrics
        .AddProcessInstrumentation()          // Process metrics  
        .AddAspNetCoreInstrumentation()       // HTTP server metrics
        .AddOtlpExporter()                    // Export to collector
        .AddPrometheusExporter());            // Direct Prometheus export
```

This gives you comprehensive .NET runtime observability with zero custom code!

## 📋 Metrics Sources Summary

| **Metric Source** | **Configuration Required** | **Custom Code Required** | **Examples** |
|-------------------|---------------------------|-------------------------|--------------|
| **OpenTelemetry Runtime** | `.AddRuntimeInstrumentation()` | ❌ No | `process_runtime_dotnet_gc_*`, `process_runtime_dotnet_jit_*` |
| **OpenTelemetry Process** | `.AddProcessInstrumentation()` | ❌ No | `process_cpu_seconds_total`, `process_*_memory_bytes` |
| **OpenTelemetry HTTP** | `.AddAspNetCoreInstrumentation()` | ❌ No | `http_server_request_duration` |
| **Custom Application** | `.AddMeter("AppName")` | ✅ Yes | `function_requests_total`, `dotnet_managed_memory_bytes` |

**Key Takeaway**: Most metrics (10+ out of 15+ total) come from OpenTelemetry instrumentation packages with zero custom code - just NuGet packages and configuration!

## Package Dependencies

The application uses the following OpenTelemetry packages for metrics collection:

### **Core OpenTelemetry packages:**
- `OpenTelemetry` (1.9.0): Core OpenTelemetry SDK
- `OpenTelemetry.Extensions.Hosting` (1.9.0): Hosting extensions for dependency injection

### **Automatic instrumentation packages (OOTB metrics):**
- `OpenTelemetry.Instrumentation.Runtime` (1.9.0): .NET runtime metrics (GC, JIT, threads, etc.)
- `OpenTelemetry.Instrumentation.Process` (0.5.0-beta.6): Process-level metrics (CPU, memory)
- `OpenTelemetry.Instrumentation.AspNetCore` (1.9.0): ASP.NET Core HTTP metrics
- `OpenTelemetry.Instrumentation.Http` (1.9.0): HTTP client instrumentation

### **Exporters:**
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` (1.9.0): OTLP export to OTEL Collector
- `OpenTelemetry.Exporter.Prometheus.AspNetCore` (1.9.0-beta.1): Direct Prometheus export

## Tracing

The application creates traces for:
- HTTP requests
- Function executions
- Custom activities with tags

## Configuration

### OpenTelemetry Endpoints (via OTEL Collector)
- **Traces**: `otel-collector:4317` (gRPC), `otel-collector:4318` (HTTP)
- **Metrics**: `otel-collector:4319` (gRPC), `otel-collector:4320` (HTTP)  
- **Logs**: `otel-collector:4321` (gRPC), `otel-collector:4322` (HTTP)

**Current Configuration**: The application sends traces to port 4317 and metrics to port 4319.

### Environment Variables
- `OTEL_EXPORTER_OTLP_ENDPOINT`: OTLP endpoint URL
- `OTEL_SERVICE_NAME`: Service name for telemetry
- `OTEL_SERVICE_VERSION`: Service version

## Grafana Dashboards

After starting the services, you can create dashboards in Grafana to visualize:
- Function request rates and latencies
- .NET runtime metrics (GC, JIT, memory, threads)
- CPU and memory usage trends
- Distributed traces
- Error rates and patterns

### Recommended Dashboard Queries

```promql
# Request rate
rate(function_requests_total[5m])

# Memory usage
process_runtime_dotnet_gc_objects_size_bytes

# GC collections
rate(process_runtime_dotnet_gc_collections_total[5m])

# CPU usage
rate(process_cpu_seconds_total[5m])

# Thread count
process_runtime_dotnet_thread_pool_threads_count
```

## Troubleshooting

1. **Check OTEL Collector logs:**
   ```bash
   docker logs azfunctionotel-otel-collector-1
   ```

2. **Verify metrics are being exported:**
   ```bash
   curl http://localhost:8889/metrics | grep "process_runtime\|dotnet_"
   ```

3. **Check function app logs:**
   ```bash
   docker logs azfunctionotel-functionapp1-1
   ```

4. **Test runtime metrics collection:**
   ```powershell
   .\test-telemetry.ps1
   ```
