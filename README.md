# Azure Function with OpenTelemetry

This project demonstrates how to collect metrics, traces, and logs from an Azure Function using OpenTelemetry.

## Components

### Function App
- **Function1**: Main HTTP trigger function with custom metrics and tracing
- **HealthFunction**: Health check endpoint
- **MetricsFunction**: Metrics information endpoint

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
   
   # Metrics info
   curl http://localhost:7071/api/Metrics
   ```

3. **Access monitoring interfaces:**
   - **Grafana**: http://localhost:3000
   - **Prometheus**: http://localhost:9091
   - **OTEL Collector metrics**: http://localhost:8889/metrics
   - **OTEL Collector zpages**: http://localhost:55679

## Custom Metrics

The function app exports the following custom metrics:

- `function_requests_total`: Counter of total function requests
- `function_request_duration_ms`: Histogram of request durations
- `health_checks_total`: Counter of health check requests
- `metrics_requests_total`: Counter of metrics endpoint requests
- `system_memory_usage_bytes`: Current memory usage
- `process_start_time_seconds`: Process start time

## Tracing

The application creates traces for:
- HTTP requests
- Function executions
- Custom activities with tags

## Configuration

### OpenTelemetry Endpoints
- Traces: `otel-collector:4317` (gRPC), `otel-collector:4318` (HTTP)
- Metrics: `otel-collector:4319` (gRPC), `otel-collector:4320` (HTTP)
- Logs: `otel-collector:4321` (gRPC), `otel-collector:4322` (HTTP)

### Environment Variables
- `OTEL_EXPORTER_OTLP_ENDPOINT`: OTLP endpoint URL
- `OTEL_SERVICE_NAME`: Service name for telemetry
- `OTEL_SERVICE_VERSION`: Service version

## Grafana Dashboards

After starting the services, you can create dashboards in Grafana to visualize:
- Function request rates and latencies
- Memory usage trends
- Distributed traces
- Error rates and patterns

## Troubleshooting

1. **Check OTEL Collector logs:**
   ```bash
   docker logs azfunctionotel-otel-collector-1
   ```

2. **Verify metrics are being exported:**
   ```bash
   curl http://localhost:8889/metrics
   ```

3. **Check function app logs:**
   ```bash
   docker logs azfunctionotel-functionapp1-1
   ```
