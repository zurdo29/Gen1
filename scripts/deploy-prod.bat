@echo off
setlocal enabledelayedexpansion

REM Production Deployment Script for Procedural Mini Game Generator
echo 🚀 Starting production deployment...

REM Configuration
set COMPOSE_FILE=docker-compose.prod.yml
set ENV_FILE=.env.production.local
set BACKUP_DIR=backups\%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%%time:~6,2%
set BACKUP_DIR=%BACKUP_DIR: =0%

REM Functions
:log_info
echo [INFO] %~1
goto :eof

:log_warn
echo [WARN] %~1
goto :eof

:log_error
echo [ERROR] %~1
goto :eof

:check_prerequisites
call :log_info "Checking prerequisites..."

REM Check if Docker is installed and running
docker --version >nul 2>&1
if errorlevel 1 (
    call :log_error "Docker is not installed"
    exit /b 1
)

docker info >nul 2>&1
if errorlevel 1 (
    call :log_error "Docker is not running"
    exit /b 1
)

REM Check if Docker Compose is available
docker-compose --version >nul 2>&1
if errorlevel 1 (
    docker compose version >nul 2>&1
    if errorlevel 1 (
        call :log_error "Docker Compose is not available"
        exit /b 1
    )
)

REM Check if environment file exists
if not exist "%ENV_FILE%" (
    call :log_warn "Environment file %ENV_FILE% not found"
    call :log_info "Creating from template..."
    copy .env.production "%ENV_FILE%"
    call :log_warn "Please edit %ENV_FILE% with your production values before continuing"
    exit /b 1
)

call :log_info "Prerequisites check passed ✓"
goto :eof

:create_directories
call :log_info "Creating necessary directories..."

if not exist logs mkdir logs
if not exist temp mkdir temp
if not exist exports mkdir exports
if not exist certs mkdir certs
if not exist nginx\logs mkdir nginx\logs
if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%"

call :log_info "Directories created ✓"
goto :eof

:backup_current_deployment
call :log_info "Creating backup of current deployment..."

REM Backup logs
if exist logs (
    xcopy logs "%BACKUP_DIR%\logs\" /E /I /Q >nul
)

REM Backup configuration
if exist "%ENV_FILE%" (
    copy "%ENV_FILE%" "%BACKUP_DIR%\" >nul
)

REM Export current container state
docker-compose -f "%COMPOSE_FILE%" ps > "%BACKUP_DIR%\container_status.txt" 2>nul

call :log_info "Backup created at %BACKUP_DIR% ✓"
goto :eof

:build_images
call :log_info "Building production images..."

REM Build images with no cache for production
docker-compose -f "%COMPOSE_FILE%" build --no-cache --parallel
if errorlevel 1 (
    call :log_error "Failed to build images"
    exit /b 1
)

call :log_info "Images built successfully ✓"
goto :eof

:run_health_checks
call :log_info "Running health checks..."

REM Wait for services to be ready
timeout /t 30 /nobreak >nul

REM Check backend health
curl -f http://localhost/api/health >nul 2>&1
if errorlevel 1 (
    call :log_error "Backend health check failed"
    exit /b 1
)
call :log_info "Backend health check passed ✓"

REM Check frontend health
curl -f http://localhost/health >nul 2>&1
if errorlevel 1 (
    call :log_error "Frontend health check failed"
    exit /b 1
)
call :log_info "Frontend health check passed ✓"

REM Check detailed backend health
curl -f http://localhost/api/health/detailed >nul 2>&1
if errorlevel 1 (
    call :log_warn "Detailed health check failed (non-critical)"
) else (
    call :log_info "Detailed health check passed ✓"
)

call :log_info "Health checks completed ✓"
goto :eof

:deploy
call :log_info "Deploying to production..."

REM Stop existing containers
docker-compose -f "%COMPOSE_FILE%" down --remove-orphans

REM Start new containers
docker-compose -f "%COMPOSE_FILE%" up -d
if errorlevel 1 (
    call :log_error "Failed to start containers"
    exit /b 1
)

REM Wait for services to start
call :log_info "Waiting for services to start..."
timeout /t 10 /nobreak >nul

REM Check if containers are running
docker-compose -f "%COMPOSE_FILE%" ps | findstr "Up" >nul
if errorlevel 1 (
    call :log_error "Some containers failed to start"
    docker-compose -f "%COMPOSE_FILE%" logs
    exit /b 1
)

call :log_info "Deployment completed ✓"
goto :eof

:cleanup
call :log_info "Cleaning up..."

REM Remove unused images
docker image prune -f >nul

REM Remove old backups (keep last 10)
for /f "skip=10 delims=" %%i in ('dir /b /ad /o-d backups\20* 2^>nul') do (
    rmdir /s /q "backups\%%i" 2>nul
)

call :log_info "Cleanup completed ✓"
goto :eof

:rollback
call :log_error "Deployment failed. Rolling back..."

REM Stop current containers
docker-compose -f "%COMPOSE_FILE%" down --remove-orphans

call :log_error "Rollback completed"
exit /b 1

REM Main deployment process
:main
call :log_info "Production Deployment Started"
call :log_info "Timestamp: %date% %time%"

call :check_prerequisites
if errorlevel 1 goto :rollback

call :create_directories
if errorlevel 1 goto :rollback

call :backup_current_deployment
if errorlevel 1 goto :rollback

call :build_images
if errorlevel 1 goto :rollback

call :deploy
if errorlevel 1 goto :rollback

call :run_health_checks
if errorlevel 1 goto :rollback

call :cleanup

call :log_info "🎉 Production deployment completed successfully!"
call :log_info "Application is available at: http://localhost"
call :log_info "API documentation: http://localhost/api"
call :log_info "Health check: http://localhost/api/health"

echo.
call :log_info "Running containers:"
docker-compose -f "%COMPOSE_FILE%" ps

goto :eof

REM Run main function
call :main