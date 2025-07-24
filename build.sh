#!/bin/bash
# Procedural Mini-game Generator Build Script for Unix/Linux/macOS
# Usage: ./build.sh [options]

set -e  # Exit on any error

# Default values
OUTPUT_PATH=""
LEVEL_PATH=""
TARGET_PLATFORM="Linux"
CONFIGURATION="Release"
SELF_CONTAINED="true"
CLEAN_BUILD="true"
TIMEOUT=300
VERBOSE="false"

# Function to show help
show_help() {
    echo "Procedural Mini-game Generator - Build Script"
    echo "=========================================="
    echo ""
    echo "Usage: ./build.sh [options]"
    echo ""
    echo "Options:"
    echo "  -h, --help              Show this help message"
    echo "  -o, --output <path>     Output path for executable (required)"
    echo "  -l, --level <path>      Path to level file to include"
    echo "  -t, --target <platform> Target platform (Windows, Linux, MacOS)"
    echo "  -c, --configuration <config> Build configuration (Debug, Release)"
    echo "  --no-self-contained     Create framework-dependent executable"
    echo "  --no-clean              Skip cleaning before build"
    echo "  --timeout <seconds>     Build timeout in seconds (default: 300)"
    echo "  --verbose               Enable verbose output"
    echo ""
    echo "Examples:"
    echo "  ./build.sh -o game -l level.json"
    echo "  ./build.sh -o game.exe -t Windows"
    echo "  ./build.sh -o game -c Debug --verbose"
    exit 0
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            show_help
            ;;
        -o|--output)
            OUTPUT_PATH="$2"
            shift 2
            ;;
        -l|--level)
            LEVEL_PATH="$2"
            shift 2
            ;;
        -t|--target)
            TARGET_PLATFORM="$2"
            shift 2
            ;;
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        --no-self-contained)
            SELF_CONTAINED="false"
            shift
            ;;
        --no-clean)
            CLEAN_BUILD="false"
            shift
            ;;
        --timeout)
            TIMEOUT="$2"
            shift 2
            ;;
        --verbose)
            VERBOSE="true"
            shift
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information."
            exit 1
            ;;
    esac
done

# Validate required parameters
if [[ -z "$OUTPUT_PATH" ]]; then
    echo "ERROR: Output path is required. Use -o or --output to specify."
    echo "Use --help for usage information."
    exit 1
fi

# Check if .NET SDK is available
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET SDK not found. Please install .NET SDK."
    exit 1
fi

echo "Procedural Mini-game Generator - Build System"
echo "============================================"
echo ""

if [[ "$VERBOSE" == "true" ]]; then
    echo "Configuration:"
    echo "  Output Path: $OUTPUT_PATH"
    echo "  Level Path: $LEVEL_PATH"
    echo "  Target Platform: $TARGET_PLATFORM"
    echo "  Configuration: $CONFIGURATION"
    echo "  Self-Contained: $SELF_CONTAINED"
    echo "  Clean Build: $CLEAN_BUILD"
    echo "  Timeout: ${TIMEOUT}s"
    echo ""
fi

# Clean build if requested
if [[ "$CLEAN_BUILD" == "true" ]]; then
    echo "Cleaning previous build artifacts..."
    if ! dotnet clean --configuration "$CONFIGURATION" --verbosity quiet; then
        echo "WARNING: Clean operation failed, continuing..."
    fi
fi

# First, build the project to ensure the command-line tool is available
echo "Building project..."
BUILD_ARGS="--configuration $CONFIGURATION"

if [[ "$VERBOSE" == "true" ]]; then
    BUILD_ARGS="$BUILD_ARGS --verbosity normal"
else
    BUILD_ARGS="$BUILD_ARGS --verbosity quiet"
fi

if ! timeout "$TIMEOUT" dotnet build $BUILD_ARGS; then
    echo "ERROR: Build failed or timed out."
    exit 1
fi

# Use the built-in command-line build tool
echo "Using integrated build system..."
BUILD_TOOL_ARGS="build -o \"$OUTPUT_PATH\" -t \"$TARGET_PLATFORM\" -c \"$CONFIGURATION\""

if [[ -n "$LEVEL_PATH" ]]; then
    BUILD_TOOL_ARGS="$BUILD_TOOL_ARGS -l \"$LEVEL_PATH\""
fi

if [[ "$SELF_CONTAINED" == "false" ]]; then
    BUILD_TOOL_ARGS="$BUILD_TOOL_ARGS --no-self-contained"
fi

if [[ "$CLEAN_BUILD" == "false" ]]; then
    BUILD_TOOL_ARGS="$BUILD_TOOL_ARGS --no-clean"
fi

if [[ "$VERBOSE" == "true" ]]; then
    BUILD_TOOL_ARGS="$BUILD_TOOL_ARGS --verbose"
fi

BUILD_TOOL_ARGS="$BUILD_TOOL_ARGS --timeout $TIMEOUT"

# Execute the integrated build tool
if ! timeout "$TIMEOUT" dotnet run --project src --configuration "$CONFIGURATION" -- $BUILD_TOOL_ARGS; then
    echo "ERROR: Integrated build process failed."
    exit 1
fi

echo ""
echo "Build completed successfully!"
exit 0