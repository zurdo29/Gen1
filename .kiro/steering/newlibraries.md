---
inclusion: always
---

# New Library Integration Guidelines

## Library Evaluation Criteria

### Compatibility Requirements
- **Backend**: Must support .NET 8 and ASP.NET Core 8
- **Frontend**: Compatible with React 18, TypeScript 5+, and Vite
- **Core Engine**: Must work with .NET 8 class libraries

### Architecture Alignment
- **Interface-driven**: Prefer libraries that support dependency injection patterns
- **Testability**: Must be mockable and unit testable
- **Performance**: Consider impact on real-time generation and WebSocket communication
- **Security**: Evaluate for vulnerabilities, especially for web-facing components

## Integration Patterns

### Backend (.NET)
- Register services in `Program.cs` using dependency injection
- Create interface abstractions for external dependencies
- Use configuration pattern for library settings
- Implement proper error handling and logging integration

### Frontend (React/TypeScript)
- Ensure full TypeScript support with proper type definitions
- Integrate with existing React Query patterns for API state
- Follow Material-UI theming conventions
- Consider bundle size impact on application performance

### Core Engine
- Maintain interface-driven architecture
- Ensure compatibility with existing validation patterns
- Consider impact on procedural generation performance

## Testing Requirements

- **Unit Tests**: All new library integrations must have test coverage
- **Integration Tests**: Test library interactions with existing systems
- **Mocking**: Ensure libraries can be properly mocked for testing
- **E2E Tests**: Update Cypress tests if library affects user workflows

## Documentation Standards

- Update relevant documentation in `docs/` directory
- Include configuration examples and common usage patterns
- Document any breaking changes or migration requirements
- Add library-specific troubleshooting information

## Security and Maintenance

- **Vulnerability Scanning**: Verify library has active security maintenance
- **License Compatibility**: Ensure license is compatible with project requirements
- **Long-term Support**: Prefer libraries with stable maintenance and community
- **Update Strategy**: Plan for regular updates and security patches

## Common Library Categories

### Backend Libraries
- **Validation**: Integrate with existing JSON schema validation
- **Caching**: Work with IMemoryCache patterns
- **Background Jobs**: Compatible with Hangfire architecture
- **Logging**: Integrate with Serilog structured logging

### Frontend Libraries
- **UI Components**: Should complement Material-UI design system
- **State Management**: Integrate with React Query patterns
- **Utilities**: Ensure TypeScript compatibility and tree-shaking support
- **Testing**: Compatible with Vitest and React Testing Library

## Current Technology Stack Preservation

When updating libraries, maintain compatibility with:
- ASP.NET Core 8 with nullable reference types
- React 18 with TypeScript and Vite build system
- Material-UI component library
- React Query for state management
- SignalR for real-time communication
- xUnit and Vitest testing frameworks
- Docker containerization patterns