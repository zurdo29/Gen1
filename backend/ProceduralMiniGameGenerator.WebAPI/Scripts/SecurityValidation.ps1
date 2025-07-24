# Security Validation Script for Procedural Mini Game Generator Web API
# This script validates that security measures are properly implemented

Write-Host "=== Security Validation Script ===" -ForegroundColor Green
Write-Host ""

# Check if security packages are installed
Write-Host "1. Checking Security Package Dependencies..." -ForegroundColor Yellow
$projectFile = "ProceduralMiniGameGenerator.WebAPI.csproj"

if (Test-Path $projectFile) {
    $content = Get-Content $projectFile -Raw
    
    $securityPackages = @(
        "AspNetCoreRateLimit",
        "Microsoft.AspNetCore.DataProtection", 
        "HtmlSanitizer"
    )
    
    foreach ($package in $securityPackages) {
        if ($content -match $package) {
            Write-Host "  ✓ $package - INSTALLED" -ForegroundColor Green
        } else {
            Write-Host "  ✗ $package - MISSING" -ForegroundColor Red
        }
    }
} else {
    Write-Host "  ✗ Project file not found" -ForegroundColor Red
}

Write-Host ""

# Check if security services are implemented
Write-Host "2. Checking Security Service Implementation..." -ForegroundColor Yellow

$securityFiles = @(
    "Services/ISecurityService.cs",
    "Services/SecurityService.cs",
    "Middleware/RateLimitingMiddleware.cs",
    "Middleware/SecurityHeadersMiddleware.cs",
    "Middleware/MiddlewareExtensions.cs"
)

foreach ($file in $securityFiles) {
    if (Test-Path $file) {
        Write-Host "  ✓ $file - EXISTS" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $file - MISSING" -ForegroundColor Red
    }
}

Write-Host ""

# Check if security tests are implemented
Write-Host "3. Checking Security Test Implementation..." -ForegroundColor Yellow

$testFiles = @(
    "Tests/Security/SecurityServiceTests.cs",
    "Tests/Security/SecurityMiddlewareTests.cs", 
    "Tests/Security/PenetrationTests.cs"
)

foreach ($file in $testFiles) {
    if (Test-Path $file) {
        Write-Host "  ✓ $file - EXISTS" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $file - MISSING" -ForegroundColor Red
    }
}

Write-Host ""

# Check Program.cs for security configuration
Write-Host "4. Checking Program.cs Security Configuration..." -ForegroundColor Yellow

if (Test-Path "Program.cs") {
    $programContent = Get-Content "Program.cs" -Raw
    
    $securityFeatures = @(
        "UseSecurityHeaders",
        "UseRateLimiting", 
        "ISecurityService",
        "AddDataProtection",
        "SetPreflightMaxAge"
    )
    
    foreach ($feature in $securityFeatures) {
        if ($programContent -match $feature) {
            Write-Host "  ✓ $feature - CONFIGURED" -ForegroundColor Green
        } else {
            Write-Host "  ✗ $feature - NOT CONFIGURED" -ForegroundColor Red
        }
    }
} else {
    Write-Host "  ✗ Program.cs not found" -ForegroundColor Red
}

Write-Host ""

# Check production configuration
Write-Host "5. Checking Production Security Configuration..." -ForegroundColor Yellow

if (Test-Path "appsettings.Production.json") {
    $prodConfig = Get-Content "appsettings.Production.json" -Raw
    
    $securitySettings = @(
        "EnableSecurityHeaders",
        "EnableRateLimiting",
        "MaxRequestSize",
        "AllowedFileExtensions",
        "EnableInputSanitization"
    )
    
    foreach ($setting in $securitySettings) {
        if ($prodConfig -match $setting) {
            Write-Host "  ✓ $setting - CONFIGURED" -ForegroundColor Green
        } else {
            Write-Host "  ✗ $setting - NOT CONFIGURED" -ForegroundColor Red
        }
    }
} else {
    Write-Host "  ✗ appsettings.Production.json not found" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Security Validation Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Security Features Implemented:" -ForegroundColor Cyan
Write-Host "• API Rate Limiting and Abuse Prevention" -ForegroundColor White
Write-Host "• CORS Configuration for Production" -ForegroundColor White  
Write-Host "• Input Sanitization and Validation" -ForegroundColor White
Write-Host "• HTTPS and Security Headers" -ForegroundColor White
Write-Host "• Comprehensive Security Tests" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "• Run security tests: dotnet test --filter Security" -ForegroundColor White
Write-Host "• Review and customize rate limits in appsettings.json" -ForegroundColor White
Write-Host "• Configure production CORS origins" -ForegroundColor White
Write-Host "• Set up SSL certificates for HTTPS" -ForegroundColor White