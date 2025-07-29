@echo off
echo Starting Azure Function with OpenTelemetry...
docker-compose -f docker-compose.yml -f docker-compose.override.yml -f docker-compose.otel.yml up --build
pause
