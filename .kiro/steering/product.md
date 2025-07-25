# Product Overview

## Procedural Mini Game Generator

A comprehensive system for procedural level generation with a modern web interface. The system consists of:

- **Core Generation Engine**: Interface-driven C# library for procedural content generation
- **Web API**: ASP.NET Core 8 backend providing RESTful endpoints and real-time updates
- **Web Interface**: React 18 + TypeScript frontend for intuitive level editing and configuration
- **Build Integration**: Automated game executable generation and deployment

## Key Features

- **Procedural Generation**: Terrain, entity placement, and complete level assembly
- **Real-time Preview**: Live level generation with WebSocket updates
- **Configuration Management**: JSON-based parameter configuration with validation
- **Export Capabilities**: Multiple output formats for game engines
- **Background Processing**: Long-running generation tasks with progress tracking
- **Comprehensive Testing**: Unit, integration, and E2E test coverage

## Target Users

- Game developers needing procedural content generation
- Level designers requiring rapid prototyping tools
- Indie developers seeking automated level creation workflows

## Architecture Philosophy

- Interface-driven design for extensibility
- Separation of concerns between generation logic and presentation
- Real-time collaboration capabilities
- Container-first deployment strategy