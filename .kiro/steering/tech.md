# Technology Stack

## Backend
- **Framework**: ASP.NET Core 8 (.NET 8)
- **Language**: C# with nullable reference types enabled
- **API**: RESTful Web API with Swagger/OpenAPI documentation
- **Real-time**: SignalR for WebSocket communication
- **Background Jobs**: Hangfire with memory storage
- **Logging**: Serilog with structured logging (console + file)
- **Caching**: In-memory caching with IMemoryCache
- **Security**: CORS, rate limiting, data protection, HTML sanitization
- **Testing**: xUnit, Moq for mocking

## Frontend
- **Framework**: React 18 with TypeScript
- **Build Tool**: Vite for fast development and building
- **UI Library**: Material-UI (@mui/material)
- **State Management**: React Query (@tanstack/react-query) for API state
- **Routing**: React Router DOM
- **Real-time**: SignalR client (@microsoft/signalr)
- **Testing**: Vitest, React Testing Library, Cypress for E2E
- **Mocking**: MSW (Mock Service Worker)
- **Linting**: ESLint with TypeScript rules

## Core Engine
- **Language**: C# with interface-driven architecture
- **Patterns**: Dependency injection, repository pattern, strategy pattern
- **Validation**: JSON schema validation with AJV

## Infrastructure
- **Containerization**: Docker with multi-stage builds
- **Development**: Docker Compose with hot reload
- **CI/CD**: GitHub Actions
- **Monitoring**: Health checks, Hangfire dashboard
- **Reverse Proxy**: Nginx for production

## Common Commands

### Development Setup
```bash
# Start full development environment
scripts/dev-start.bat          # Windows
./scripts/dev-start.sh         # Linux/Mac

# Backend only (local development)
cd backend/ProceduralMiniGameGenerator.WebAPI
dotnet restore
dotnet run

# Frontend only (local development)
cd frontend
npm install
npm run dev
```

### Building
```bash
# Build all components
npm run build:all

# Backend build
cd backend && dotnet build --configuration Release

# Frontend build
cd frontend && npm run build

# Docker production build
docker-compose -f docker-compose.yml build
```

### Testing
```bash
# Run all tests
npm run test:comprehensive

# Backend tests
cd backend && dotnet test

# Frontend unit tests
cd frontend && npm run test

# E2E tests
cd frontend && npm run test:e2e

# Integration tests
npm run test:backend
```

### Development URLs
- Frontend: http://localhost:3000
- Backend API: http://localhost:5000
- API Documentation: http://localhost:5000/swagger
- Hangfire Dashboard: http://localhost:5000/hangfire

## Configuration Patterns

### Environment Variables
- Use `.env.example` as template
- Separate configs for development/production
- CORS origins configurable via `CorsOrigins` array
- API proxy configured in Vite for development

### JSON Serialization
- camelCase property naming
- String enum conversion
- Null value ignoring
- Trailing commas allowed