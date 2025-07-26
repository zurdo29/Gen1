# Development Setup Guide

## Quick Start (With Known Issues)

This project is currently in active development with some known issues. Follow this guide for the most stable development experience.

### Prerequisites
- Node.js 20.19.0 or higher
- .NET 8 SDK
- Docker (optional, for containerized development)

### Backend Setup
```bash
cd backend/ProceduralMiniGameGenerator.WebAPI
dotnet restore
dotnet build
dotnet run
```

**Note**: Backend will build with warnings but is functional.

### Frontend Setup
```bash
cd frontend
npm install
npm run dev
```

**Note**: TypeScript errors are present but the application will run in development mode.

## Current Development Status

### ✅ Working Features
- Backend API core functionality
- Frontend development server
- Basic level generation
- Configuration management
- Docker containerization

### ⚠️ Known Issues
- 223 TypeScript errors in frontend (see KNOWN_ISSUES.md)
- 2 C# compilation errors in backend
- Test suite not fully functional
- Some components missing implementations

### 🔧 Workarounds

#### For Frontend Development
```bash
# Start development server (ignores TypeScript errors)
npm run dev

# Build for production (may fail)
npm run build

# Skip type checking during development
npm run dev -- --no-type-check
```

#### For Backend Development
```bash
# Build with minimal output
dotnet build --verbosity quiet

# Run with development settings
dotnet run --environment Development
```

## Project Structure

```
├── backend/                    # ASP.NET Core Web API
│   ├── ProceduralMiniGameGenerator.WebAPI/
│   └── ProceduralMiniGameGenerator.WebAPI.Tests/
├── frontend/                   # React TypeScript application
│   ├── src/
│   ├── public/
│   └── package.json
├── src/                       # Core generation engine (C# library)
├── docs/                      # Project documentation
└── docker-compose*.yml       # Container orchestration
```

## Development Workflow

### 1. Backend Development
```bash
cd backend/ProceduralMiniGameGenerator.WebAPI
dotnet watch run
```

### 2. Frontend Development
```bash
cd frontend
npm run dev
```

### 3. Full Stack Development
```bash
# Terminal 1 - Backend
cd backend/ProceduralMiniGameGenerator.WebAPI && dotnet watch run

# Terminal 2 - Frontend
cd frontend && npm run dev
```

### 4. Docker Development
```bash
docker-compose -f docker-compose.dev.yml up
```

## API Endpoints

- **Backend**: http://localhost:5000
- **Frontend**: http://localhost:3000
- **API Documentation**: http://localhost:5000/swagger

## Contributing

### Before Contributing
1. Review KNOWN_ISSUES.md
2. Check if your issue is already documented
3. Test your changes with both backend and frontend

### Code Style
- **Backend**: Follow C# conventions, use nullable reference types
- **Frontend**: Follow TypeScript/React best practices
- **Tests**: Use Vitest for frontend, xUnit for backend

### Pull Request Process
1. Create feature branch from `main`
2. Document any new known issues
3. Update relevant documentation
4. Test with both development and production builds

## Troubleshooting

### Common Issues

#### "Cannot find module" errors
```bash
cd frontend
rm -rf node_modules package-lock.json
npm install
```

#### Backend compilation errors
```bash
cd backend/ProceduralMiniGameGenerator.WebAPI
dotnet clean
dotnet restore
dotnet build
```

#### Docker issues
```bash
docker-compose down
docker system prune -f
docker-compose up --build
```

### Getting Help
1. Check KNOWN_ISSUES.md first
2. Review existing GitHub issues
3. Check the troubleshooting guide in docs/
4. Create a new issue with detailed reproduction steps

## Roadmap

### Phase 1: Stabilization
- [ ] Fix TypeScript compilation errors
- [ ] Resolve backend nullable reference issues
- [ ] Implement missing components
- [ ] Fix test suite

### Phase 2: Feature Completion
- [ ] Complete real-time preview functionality
- [ ] Implement batch generation
- [ ] Add comprehensive validation
- [ ] Improve error handling

### Phase 3: Production Ready
- [ ] Performance optimization
- [ ] Security hardening
- [ ] Comprehensive testing
- [ ] Deployment automation