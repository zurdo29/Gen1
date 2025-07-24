@echo off
echo Testing Web Project Setup...

echo.
echo 1. Testing Backend Build...
cd backend\ProceduralMiniGameGenerator.WebAPI
dotnet build
if %ERRORLEVEL% NEQ 0 (
    echo Backend build FAILED
    pause
    exit /b 1
)
echo Backend build SUCCESS

echo.
echo 2. Testing Frontend Build...
cd ..\..\frontend
call npm run build
if %ERRORLEVEL% NEQ 0 (
    echo Frontend build FAILED
    pause
    exit /b 1
)
echo Frontend build SUCCESS

echo.
echo 3. Testing Frontend Tests...
call npm run test
if %ERRORLEVEL% NEQ 0 (
    echo Frontend tests FAILED
    pause
    exit /b 1
)
echo Frontend tests SUCCESS

echo.
echo 4. Testing Docker Compose Configuration...
cd ..
docker-compose config
if %ERRORLEVEL% NEQ 0 (
    echo Docker compose config FAILED
    pause
    exit /b 1
)
echo Docker compose config SUCCESS

echo.
echo ========================================
echo All tests PASSED! Web project setup is complete.
echo ========================================
echo.
echo To start development environment:
echo   scripts\dev-start.bat
echo.
echo To start production environment:
echo   docker-compose up --build
echo.
pause