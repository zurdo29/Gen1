@echo off
REM Procedural Mini-game Generator Build Script for Windows
REM Usage: build.bat [options]

setlocal enabledelayedexpansion

REM Default values
set OUTPUT_PATH=
set LEVEL_PATH=
set TARGET_PLATFORM=Windows
set CONFIGURATION=Release
set SELF_CONTAINED=true
set CLEAN_BUILD=true
set TIMEOUT=300
set VERBOSE=false

REM Parse command line arguments
:parse_args
if "%~1"=="" goto :end_parse
if "%~1"=="-h" goto :show_help
if "%~1"=="--help" goto :show_help
if "%~1"=="-o" (
    set OUTPUT_PATH=%~2
    shift
    shift
    goto :parse_args
)
if "%~1"=="--output" (
    set OUTPUT_PATH=%~2
    shift
    shift
    goto :parse_args
)
if "%~1"=="-l" (
    set LEVEL_PATH=%~2
    shift
    shift
    goto :parse_args
)
if "%~1"=="--level" (
    set LEVEL_PATH=%~2
    shift
    shift
    goto :parse_args
)
if "%~1"=="-t" (
    set TARGET_PLATFORM=%~2
    shift
    shift
    goto :parse_args
)
if "%~1"=="--target" (
    set TARGET_PLATFORM=%~2
    shift
    shift
    goto :parse_args
)
if "%~1"=="-c" (
    set CONFIGURATION=%~2
    shift
    shift
    goto :parse_args
)
if "%~1"=="--configuration" (
    set CONFIGURATION=%~2
    shift
    shift
    goto :parse_args
)
if "%~1"=="--no-self-contained" (
    set SELF_CONTAINED=false
    shift
    goto :parse_args
)
if "%~1"=="--no-clean" (
    set CLEAN_BUILD=false
    shift
    goto :parse_args
)
if "%~1"=="--timeout" (
    set TIMEOUT=%~2
    shift
    shift
    goto :parse_args
)
if "%~1"=="--verbose" (
    set VERBOSE=true
    shift
    goto :parse_args
)
shift
goto :parse_args

:end_parse

REM Validate required parameters
if "%OUTPUT_PATH%"=="" (
    echo ERROR: Output path is required. Use -o or --output to specify.
    echo Use --help for usage information.
    exit /b 1
)

REM Check if .NET SDK is available
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK not found. Please install .NET SDK.
    exit /b 1
)

echo Procedural Mini-game Generator - Build System
echo ============================================
echo.

if "%VERBOSE%"=="true" (
    echo Configuration:
    echo   Output Path: %OUTPUT_PATH%
    echo   Level Path: %LEVEL_PATH%
    echo   Target Platform: %TARGET_PLATFORM%
    echo   Configuration: %CONFIGURATION%
    echo   Self-Contained: %SELF_CONTAINED%
    echo   Clean Build: %CLEAN_BUILD%
    echo   Timeout: %TIMEOUT%s
    echo.
)

REM Clean build if requested
if "%CLEAN_BUILD%"=="true" (
    echo Cleaning previous build artifacts...
    dotnet clean --configuration %CONFIGURATION% --verbosity quiet
    if errorlevel 1 (
        echo WARNING: Clean operation failed, continuing...
    )
)

REM First, build the project to ensure the command-line tool is available
echo Building project...
set BUILD_ARGS=--configuration %CONFIGURATION%

if "%VERBOSE%"=="true" (
    set BUILD_ARGS=%BUILD_ARGS% --verbosity normal
) else (
    set BUILD_ARGS=%BUILD_ARGS% --verbosity quiet
)

dotnet build %BUILD_ARGS%
if errorlevel 1 (
    echo ERROR: Build failed.
    exit /b 1
)

REM Use the built-in command-line build tool
echo Using integrated build system...
set BUILD_TOOL_ARGS=build -o "%OUTPUT_PATH%" -t "%TARGET_PLATFORM%" -c "%CONFIGURATION%"

if not "%LEVEL_PATH%"=="" (
    set BUILD_TOOL_ARGS=%BUILD_TOOL_ARGS% -l "%LEVEL_PATH%"
)

if "%SELF_CONTAINED%"=="false" (
    set BUILD_TOOL_ARGS=%BUILD_TOOL_ARGS% --no-self-contained
)

if "%CLEAN_BUILD%"=="false" (
    set BUILD_TOOL_ARGS=%BUILD_TOOL_ARGS% --no-clean
)

if "%VERBOSE%"=="true" (
    set BUILD_TOOL_ARGS=%BUILD_TOOL_ARGS% --verbose
)

set BUILD_TOOL_ARGS=%BUILD_TOOL_ARGS% --timeout %TIMEOUT%

REM Execute the integrated build tool
dotnet run --project src --configuration %CONFIGURATION% -- %BUILD_TOOL_ARGS%
if errorlevel 1 (
    echo ERROR: Integrated build process failed.
    exit /b 1
)

echo.
echo Build completed successfully!
exit /b 0

:show_help
echo Procedural Mini-game Generator - Build Script
echo ==========================================
echo.
echo Usage: build.bat [options]
echo.
echo Options:
echo   -h, --help              Show this help message
echo   -o, --output ^<path^>     Output path for executable (required)
echo   -l, --level ^<path^>      Path to level file to include
echo   -t, --target ^<platform^> Target platform (Windows, Linux, MacOS)
echo   -c, --configuration ^<config^> Build configuration (Debug, Release)
echo   --no-self-contained     Create framework-dependent executable
echo   --no-clean              Skip cleaning before build
echo   --timeout ^<seconds^>     Build timeout in seconds (default: 300)
echo   --verbose               Enable verbose output
echo.
echo Examples:
echo   build.bat -o game.exe -l level.json
echo   build.bat -o game.exe -t Linux
echo   build.bat -o game.exe -c Debug --verbose
exit /b 0