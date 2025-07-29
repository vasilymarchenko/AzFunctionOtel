# Test script for Azure Function with OpenTelemetry
# This script tests the function endpoints and generates telemetry data

Write-Host "Testing Azure Function with OpenTelemetry..." -ForegroundColor Green

# Function endpoint URLs
$baseUrl = "http://localhost:7071/api"
$function1Url = "$baseUrl/Function1"
$healthUrl = "$baseUrl/Health"
$metricsUrl = "$baseUrl/Metrics"

# Test Function1 endpoint
Write-Host "`nTesting Function1 endpoint..." -ForegroundColor Yellow
try {
    for ($i = 1; $i -le 10; $i++) {
        $response = Invoke-RestMethod -Uri $function1Url -Method GET
        Write-Host "Request $i - Status: OK, RequestId: $($response.requestId)"
        Start-Sleep -Milliseconds 500
    }
} catch {
    Write-Host "Error testing Function1: $($_.Exception.Message)" -ForegroundColor Red
}

# Test Health endpoint
Write-Host "`nTesting Health endpoint..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri $healthUrl -Method GET
    Write-Host "Health Status: $($healthResponse.status), Uptime: $($healthResponse.uptime)ms"
} catch {
    Write-Host "Error testing Health endpoint: $($_.Exception.Message)" -ForegroundColor Red
}

# Test Metrics endpoint
Write-Host "`nTesting Metrics endpoint..." -ForegroundColor Yellow
try {
    $metricsResponse = Invoke-RestMethod -Uri $metricsUrl -Method GET
    Write-Host "Memory Usage: $([math]::Round($metricsResponse.memory_usage_bytes / 1MB, 2)) MB"
    Write-Host "Process Start Time: $($metricsResponse.process_start_time)"
} catch {
    Write-Host "Error testing Metrics endpoint: $($_.Exception.Message)" -ForegroundColor Red
}

# Test some POST requests to Function1
Write-Host "`nTesting Function1 with POST requests..." -ForegroundColor Yellow
try {
    for ($i = 1; $i -le 5; $i++) {
        $body = @{ test = "data"; iteration = $i } | ConvertTo-Json
        $response = Invoke-RestMethod -Uri $function1Url -Method POST -Body $body -ContentType "application/json"
        Write-Host "POST Request $i - Status: OK"
        Start-Sleep -Milliseconds 300
    }
} catch {
    Write-Host "Error testing Function1 POST: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nTelemetry test completed!" -ForegroundColor Green
Write-Host "Check the following URLs for monitoring data:" -ForegroundColor Cyan
Write-Host "- Grafana: http://localhost:3000"
Write-Host "- Prometheus: http://localhost:9091"
Write-Host "- OTEL Metrics: http://localhost:8889/metrics"
Write-Host "- OTEL ZPages: http://localhost:55679"
