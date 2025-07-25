# Project Structure

## Root Directory Organization

```
├── backend/                    # ASP.NET Core Web API
├── frontend/                   # React TypeScript application
├── src/                       # Core generation engine (C# library)
├── scripts/                   # Build and development scripts
├── docs/                      # Project documentation
├── test-runner/               # Integration test runner
├── .github/workflows/         # CI/CD pipelines
├── docker-compose*.yml        # Container orchestration
└── package.json              # Root package for test coordination
```

## Backend Structure (`backend/ProceduralMiniGameGenerator.WebAPI/`)

```
├── Controllers/               # API endpoints
├── Services/                 # Business logic services
├── Models/                   # API-specific data models
├── Middleware/               # Custom middleware components
├── Hubs/                     # SignalR hubs for real-time communication
├── Tests/                    # Unit and integration tests
├── Program.cs                # Application entry point and DI configuration
├── appsettings*.json         # Environment-specific configuration
└── Dockerfile*               # Container definitions
```

### Backend Conventions
- **Controllers**: Inherit from `ControllerBase`, use `[ApiController]` attribute
- **Services**: Interface-first design with `I{ServiceName}` pattern
- **Dependency Injection**: Register all services in `Program.cs`
- **Middleware**: Custom middleware for cross-cutting concerns
- **Logging**: Use injected `ILogger<T>` or custom `ILoggerService`
- **Error Handling**: Global exception middleware with structured responses

## Frontend Structure (`frontend/src/`)

```
├── components/               # Reusable UI components
├── pages/                   # Route-level page components
├── services/                # API client services
├── hooks/                   # Custom React hooks
├── types/                   # TypeScript type definitions
├── utils/                   # Utility functions
├── test/                    # Test utilities and setup
├── schemas/                 # JSON schema definitions
├── App.tsx                  # Main application component
└── main.tsx                 # Application entry point
```

### Frontend Conventions
- **Components**: Use TypeScript with proper prop typing
- **API Calls**: Use React Query for state management
- **Styling**: Material-UI components with emotion styling
- **Routing**: React Router with lazy loading for code splitting
- **State**: Prefer React Query for server state, useState for local state
- **Testing**: Co-locate test files with components using `.test.tsx`
- **Imports**: Use `@/` alias for src directory imports

## Core Engine Structure (`src/`)

```
├── build/                    # Build system interfaces
├── configuration/            # Configuration parsing
├── core/                    # Core system interfaces
├── editor/                  # Editor integration
├── generators/              # Generation algorithms
├── models/                  # Data models and entities
│   └── entities/           # Specific entity implementations
└── validators/             # Validation interfaces
```

### Core Engine Conventions
- **Interfaces**: All major components have corresponding interfaces
- **Naming**: Use `I{ComponentName}` for interfaces
- **Models**: Separate data models from business logic
- **Validation**: Dedicated validators for each domain
- **Generators**: Strategy pattern for different generation algorithms

## Configuration Management

### Environment Files
- `.env.example` - Template for environment variables
- `.env.development` - Development-specific settings
- `.env.production` - Production-specific settings

### JSON Configuration
- `appsettings.json` - Base backend configuration
- `appsettings.{Environment}.json` - Environment overrides
- Frontend config via environment variables prefixed with `VITE_`

## Testing Structure

### Backend Testing
- Unit tests in `*.Tests` projects
- Integration tests in `Integration/` folders
- Performance tests in `Performance/` folders
- Use xUnit framework with Moq for mocking

### Frontend Testing
- Unit tests: `*.test.tsx` files co-located with components
- Integration tests: `tests/` directory
- E2E tests: `cypress/e2e/` directory
- Accessibility tests: Separate Vitest config

## Docker Structure

### Development
- `docker-compose.dev.yml` - Development environment with hot reload
- Volume mounts for source code
- Separate containers for frontend/backend

### Production
- `docker-compose.yml` - Production environment
- Multi-stage builds for optimization
- Nginx reverse proxy configuration

## Naming Conventions

### Files and Directories
- **Backend**: PascalCase for C# files and directories
- **Frontend**: camelCase for files, PascalCase for components
- **Configuration**: kebab-case for Docker and script files

### Code
- **C#**: PascalCase for public members, camelCase for private
- **TypeScript**: camelCase for variables/functions, PascalCase for types/interfaces
- **API Endpoints**: RESTful conventions with plural nouns
- **Database/Models**: PascalCase properties, descriptive names

## Key Architectural Patterns

- **Dependency Injection**: Extensive use throughout backend
- **Repository Pattern**: Data access abstraction
- **Strategy Pattern**: Pluggable generation algorithms
- **Middleware Pipeline**: Cross-cutting concerns in ASP.NET Core
- **Component Composition**: React component hierarchy
- **Hook Pattern**: Custom React hooks for reusable logic