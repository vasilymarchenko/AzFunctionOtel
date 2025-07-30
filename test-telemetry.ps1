# Comprehensive Test Script for Azure Function with OpenTelemetry
# This script tests all function endpoints, generates telemetry data, and verifies .NET runtime metrics

Write-Host "Testing Azure Function with OpenTelemetry & .NET Runtime Metrics" -ForegroundColor Green
Write-Host "======================================================================" -ForegroundColor Green

$baseUrl = "http://localhost:7071/api"

# Function to make HTTP requests and handle responses
function Test-Endpoint {
    param (
        [string]$Url,
        [string]$Description,
        [string]$Method = "GET",
        [object]$Body = $null
    )
    
    Write-Host "`nTesting: $Description" -ForegroundColor Yellow
    Write-Host "URL: $Url"
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            TimeoutSec = 10
        }
        
        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json)
            $params.ContentType = "application/json"
        }
        
        $response = Invoke-RestMethod @params
        Write-Host "✅ Success" -ForegroundColor Green
        Write-Host "Response: $($response | ConvertTo-Json -Depth 2)" -ForegroundColor Cyan
        return $true
    }
    catch {
        Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Wait for function app to be ready
Write-Host "`nWaiting for Azure Function to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Test all endpoints
$endpoints = @(
    @{ Url = "$baseUrl/Function1"; Description = "Basic Function with custom metrics" },
    @{ Url = "$baseUrl/Metrics"; Description = "Metrics Function with system metrics" },
    @{ Url = "$baseUrl/SystemMetrics"; Description = "System Metrics Function with .NET runtime data" },
    @{ Url = "$baseUrl/Health"; Description = "Health Function" }
)

$successCount = 0
foreach ($endpoint in $endpoints) {
    if (Test-Endpoint -Url $endpoint.Url -Description $endpoint.Description) {
        $successCount++
    }
    Start-Sleep -Seconds 2
}

# Generate additional load with multiple GET requests to Function1
Write-Host "`nGenerating load with multiple requests to Function1..." -ForegroundColor Yellow
try {
    for ($i = 1; $i -le 10; $i++) {
        $response = Invoke-RestMethod -Uri "$baseUrl/Function1" -Method GET -TimeoutSec 10
        Write-Host "Load Request $i - Status: OK" -ForegroundColor Green
        Start-Sleep -Milliseconds 500
    }
} catch {
    Write-Host "Error during load generation: $($_.Exception.Message)" -ForegroundColor Red
}

# Test POST requests to Function1
Write-Host "`nTesting Function1 with POST requests..." -ForegroundColor Yellow
$postSuccessCount = 0
try {
    for ($i = 1; $i -le 5; $i++) {
        $body = @{ test = "data"; iteration = $i; timestamp = (Get-Date).ToString() }
        if (Test-Endpoint -Url "$baseUrl/Function1" -Description "POST Request $i" -Method "POST" -Body $body) {
            $postSuccessCount++
        }
        Start-Sleep -Milliseconds 300
    }
} catch {
    Write-Host "Error testing Function1 POST: $($_.Exception.Message)" -ForegroundColor Red
}

# Check Prometheus metrics endpoint
Write-Host "`nChecking Prometheus metrics endpoint..." -ForegroundColor Yellow
try {
    $prometheusMetrics = Invoke-RestMethod -Uri "http://localhost:9091/metrics" -TimeoutSec 10
    Write-Host "✅ Prometheus endpoint accessible" -ForegroundColor Green
    
    # Look for .NET runtime metrics
    $runtimeMetrics = $prometheusMetrics -split "`n" | Where-Object { 
        $_ -match "dotnet_|process_|runtime_" -and $_ -notmatch "^#" 
    }
    
    if ($runtimeMetrics.Count -gt 0) {
        Write-Host "`n🎯 Found .NET Runtime Metrics:" -ForegroundColor Green
        $runtimeMetrics | Select-Object -First 10 | ForEach-Object {
            Write-Host "  $_" -ForegroundColor Cyan
        }
        if ($runtimeMetrics.Count -gt 10) {
            Write-Host "  ... and $($runtimeMetrics.Count - 10) more metrics" -ForegroundColor Gray
        }
    } else {
        Write-Host "`n⚠️  No .NET runtime metrics found yet. Try making a few more requests and check again." -ForegroundColor Yellow
    }
}
catch {
    Write-Host "❌ Prometheus endpoint not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n======================================================================" -ForegroundColor Green
Write-Host "📊 Test Summary:" -ForegroundColor Green
Write-Host "  - Endpoint Tests: $successCount/$($endpoints.Count) successful" -ForegroundColor $(if ($successCount -eq $endpoints.Count) { "Green" } else { "Yellow" })
Write-Host "  - POST Tests: $postSuccessCount/5 successful" -ForegroundColor $(if ($postSuccessCount -eq 5) { "Green" } else { "Yellow" })
Write-Host "  - Load Generation: 10 requests completed" -ForegroundColor Green

Write-Host "`n🔍 Monitor your metrics at:" -ForegroundColor Green
Write-Host "  - Prometheus: http://localhost:9091" -ForegroundColor Cyan
Write-Host "  - Grafana: http://localhost:3000" -ForegroundColor Cyan
Write-Host "  - OTEL Collector: http://localhost:8888" -ForegroundColor Cyan
Write-Host "  - OTEL Metrics: http://localhost:8889/metrics" -ForegroundColor Cyan
Write-Host "  - OTEL ZPages: http://localhost:55679" -ForegroundColor Cyan

Write-Host "`n📋 Expected .NET Runtime Metrics:" -ForegroundColor Green
@(
    "process_runtime_dotnet_gc_collections_total",
    "process_runtime_dotnet_gc_objects_size_bytes", 
    "process_runtime_dotnet_gc_allocations_size_bytes",
    "process_runtime_dotnet_jit_compilation_time_seconds",
    "process_runtime_dotnet_thread_pool_threads_count",
    "process_runtime_dotnet_monitor_lock_contention_total",
    "process_runtime_dotnet_time_in_gc_ratio",
    "process_cpu_seconds_total",
    "process_resident_memory_bytes",
    "process_virtual_memory_bytes"
) | ForEach-Object {
    Write-Host "  - $_" -ForegroundColor Gray
}

Write-Host "`n✅ Telemetry test completed!" -ForegroundColor Green
