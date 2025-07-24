# Procedural Level Editor - Web Application

A modern web-based interface for the Procedural Mini Game Generator, providing intuitive level generation and editing capabilities.

## Architecture

- **Backend**: ASP.NET Core 8 Web API
- **Frontend**: React 18 with TypeScript and Vite
- **Containerization**: Docker with multi-stage builds
- **Development**: Hot reload for both frontend and backend

## Quick Start

### Development Environment

1. **Prerequisites**
   - Docker and Docker Compose
   - .NET 8 SDK (for local development)
   - Node.js 18+ (for local development)

2. **Start Development Environment**
   ```bash
   # Windows
   scripts\dev-start.bat
   
   # Linux/Mac
   chmod +x scripts/dev-start.sh
   ./scripts/dev-start.sh
   ```

3. **Access the Application**
   - Frontend: http://localhost:3000
   - Backend API: http://localhost:5000
   - API Documentation: http://localhost:5000/swagger
   - Hangfire Dashboard: http://localhost:5000/hangfire

### Local Development (without Docker)

1. **Backend**
   ```bash
   cd backend/ProceduralMiniGameGenerator.WebAPI
   dotnet restore
   dotnet run
   ```

2. **Frontend**
   ```bash
   cd frontend
   npm install
   npm run dev
   ```

### Production Build

```bash
# Build production containers
docker-compose -f docker-compose.yml build

# Start production environment
docker-compose -f docker-compose.yml up
```

## Project Structure

```
├── backend/
│   └── ProceduralMiniGameGenerator.WebAPI/
│       ├── Controllers/
│       ├── Program.cs
│       ├── Dockerfile
│       └── appsettings.json
├── frontend/
│   ├── src/
│   │   ├── components/
│   │   ├── services/
│   │   └── App.tsx
│   ├── Dockerfile
│   ├── nginx.conf
│   └── package.json
├── .github/workflows/
│   └── ci-cd.yml
├── scripts/
│   ├── dev-start.bat
│   └── dev-start.sh
├── docker-compose.yml
└── docker-compose.dev.yml
```

## Features

### Current Implementation
- ✅ ASP.NET Core Web API with Swagger documentation
- ✅ React TypeScript frontend with Material-UI
- ✅ Docker containerization with development and production configs
- ✅ Hot reload development environment
- ✅ CI/CD pipeline with GitHub Actions
- ✅ Health check endpoints
- ✅ Structured logging with Serilog
- ✅ Background job processing with Hangfire
- ✅ CORS configuration for frontend communication

### Planned Features
- 🔄 Level generation API endpoints
- 🔄 Real-time level preview
- 🔄 Parameter configuration interface
- 🔄 Export functionality
- 🔄 Sharing and collaboration features

## Development

### Backend Development
- Built on existing C# generation engine
- RESTful API design
- Background job processing for long-running operations
- Comprehensive logging and monitoring

### Frontend Development
- Modern React with TypeScript
- Material-UI component library
- React Query for API state management
- Canvas-based level visualization
- Responsive design for mobile and desktop

### Testing
```bash
# Backend tests
dotnet test

# Frontend tests
cd frontend
npm run test

# Run all tests in CI
npm run test:ci
```

### Environment Variables

Copy `.env.example` to `.env` and configure:

```env
# Backend
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:5000

# Frontend
VITE_API_URL=http://localhost:5000

# Docker
COMPOSE_PROJECT_NAME=procedural-level-editor
```

## Deployment

### GitHub Actions CI/CD
- Automated testing on push/PR
- Docker image building and publishing
- Deployment to staging/production environments

### Manual Deployment
1. Build production images: `docker-compose build`
2. Deploy to container hosting service (Azure Container Apps, AWS ECS, etc.)
3. Configure environment variables for production
4. Set up monitoring and logging

## Monitoring

- **Health Checks**: `/api/health`
- **Metrics**: Hangfire dashboard at `/hangfire`
- **Logs**: Structured logging with Serilog
- **API Documentation**: Swagger UI at `/swagger`

## Security

- CORS configuration for frontend domains
- Input validation and sanitization
- Rate limiting (planned)
- HTTPS enforcement in production
- Security headers via nginx

## Contributing

1. Follow the existing code structure
2. Add tests for new features
3. Update documentation
4. Ensure Docker builds work
5. Test both development and production configurations