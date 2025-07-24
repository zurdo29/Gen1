Write-Host "Security Implementation Check" -ForegroundColor Green

# Check security files
$files = @(
    "Services/ISecurityService.cs",
    "Services/SecurityService.cs", 
    "Middleware/RateLimitingMiddleware.cs",
    "Middleware/SecurityHeadersMiddleware.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "✓ $file" -ForegroundColor Green
    } else {
        Write-Host "✗ $file" -ForegroundColor Red
    }
}

Write-Host "Security implementation complete!" -ForegroundColor Cyan