# Testing Documentation

This document describes the comprehensive testing strategy for the Procedural Level Editor web application.

## Test Structure

### Unit Tests
- **Location**: `src/**/*.test.{ts,tsx}`
- **Framework**: Vitest + React Testing Library
- **Purpose**: Test individual components, hooks, and utility functions
- **Coverage Target**: 80%+ for all categories

### Integration Tests
- **Location**: `backend/ProceduralMiniGameGenerator.WebAPI.Tests/Integration/`
- **Framework**: xUnit + ASP.NET Core Test Host
- **Purpose**: Test API endpoints and service integration
- **Coverage**: All API endpoints and critical workflows

### End-to-End Tests
- **Location**: `cypress/e2e/`
- **Framework**: Cypress
- **Purpose**: Test complete user workflows
- **Coverage**: All major user journeys

### Accessibility Tests
- **Location**: `src/**/*.accessibility.test.{ts,tsx}` and `cypress/e2e/accessibility.cy.ts`
- **Framework**: axe-core + Cypress
- **Purpose**: Ensure WCAG 2.1 AA compliance
- **Coverage**: All interactive components and workflows

### Visual Regression Tests
- **Location**: `src/**/*.visual.test.{ts,tsx}`
- **Framework**: Vitest snapshots
- **Purpose**: Detect unintended visual changes
- **Coverage**: Key UI components and level rendering

### Performance Tests
- **Location**: `backend/ProceduralMiniGameGenerator.WebAPI.Tests/Performance/`
- **Framework**: NBomber
- **Purpose**: Validate performance requirements
- **Coverage**: API endpoints under load

## Running Tests

### Frontend Tests

```bash
# Run all unit tests
npm test

# Run tests in watch mode
npm run test:watch

# Run tests with coverage
npm run test:coverage

# Run accessibility tests
npm run test:accessibility

# Run tests with UI
npm run test:ui
```

### Backend Tests

```bash
# Run all backend tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter Category=Integration
```

### End-to-End Tests

```bash
# Run Cypress tests headlessly
npm run test:e2e

# Open Cypress UI
npm run cypress:open

# Run specific test file
npx cypress run --spec "cypress/e2e/level-generation.cy.ts"
```

## Test Configuration

### Vitest Configuration
- **Config File**: `vitest.config.ts`
- **Environment**: jsdom
- **Setup Files**: `src/test/setup.ts`
- **Coverage Provider**: v8
- **Thresholds**: 80% for all metrics

### Cypress Configuration
- **Config File**: `cypress.config.ts`
- **Base URL**: http://localhost:3000
- **API URL**: http://localhost:5000/api
- **Viewport**: 1280x720
- **Video Recording**: Enabled for CI

### MSW (Mock Service Worker)
- **Setup**: `src/test/mocks/server.ts`
- **Handlers**: `src/test/mocks/handlers.ts`
- **Mock Data**: `src/test/mocks/mockData.ts`

## Test Utilities

### Custom Render Function
```typescript
import { render } from '../test/utils/test-utils'

// Automatically wraps components with providers
render(<MyComponent />)
```

### Custom Cypress Commands
```typescript
// Configure level parameters
cy.configureLevel({ width: 25, height: 25 })

// Wait for generation to complete
cy.waitForGeneration()

// Check accessibility
cy.checkA11y()
```

## Test Categories

### Component Tests
- Rendering with different props
- User interactions (clicks, typing, etc.)
- State changes and side effects
- Error handling and edge cases
- Accessibility compliance

### API Tests
- Successful responses
- Error handling
- Validation
- Network failures
- Retry logic
- Timeout handling

### Integration Tests
- Full API workflows
- Database interactions
- Service dependencies
- Authentication/authorization
- File operations

### E2E Tests
- Complete user journeys
- Cross-browser compatibility
- Mobile responsiveness
- Performance under realistic conditions
- Error recovery

## Accessibility Testing

### Automated Checks
- Color contrast ratios
- Keyboard navigation
- ARIA attributes
- Semantic HTML
- Focus management

### Manual Testing Scenarios
- Screen reader compatibility
- High contrast mode
- Reduced motion preferences
- Keyboard-only navigation
- Voice control compatibility

## Performance Testing

### Metrics Tracked
- Response times (mean, p95, p99)
- Throughput (requests per second)
- Error rates
- Memory usage
- CPU utilization

### Load Scenarios
- Normal load (10 RPS)
- Peak load (50 RPS)
- Stress test (100+ RPS)
- Concurrent users (50+ simultaneous)
- Large data sets (100x100 levels)

## CI/CD Integration

### GitHub Actions
- Run all test suites on PR
- Generate coverage reports
- Run accessibility audits
- Performance regression detection
- Visual diff comparisons

### Test Reporting
- Coverage reports uploaded to Codecov
- Test results in JUnit format
- Accessibility reports
- Performance benchmarks
- Visual regression reports

## Best Practices

### Writing Tests
1. Follow AAA pattern (Arrange, Act, Assert)
2. Use descriptive test names
3. Test behavior, not implementation
4. Mock external dependencies
5. Keep tests focused and isolated

### Accessibility Testing
1. Test with keyboard only
2. Use screen reader testing tools
3. Verify color contrast
4. Test with different zoom levels
5. Validate ARIA attributes

### Performance Testing
1. Test with realistic data sizes
2. Monitor memory usage
3. Test concurrent scenarios
4. Validate response times
5. Check for memory leaks

### E2E Testing
1. Test critical user paths
2. Use stable selectors (data-testid)
3. Handle async operations properly
4. Test error scenarios
5. Keep tests maintainable

## Troubleshooting

### Common Issues
- **Flaky tests**: Add proper waits and assertions
- **Memory leaks**: Clean up resources in afterEach
- **Timeout errors**: Increase timeout for slow operations
- **Mock issues**: Verify mock setup and reset between tests
- **Accessibility violations**: Check ARIA attributes and semantic HTML

### Debug Commands
```bash
# Debug specific test
npm test -- --reporter=verbose MyComponent.test.tsx

# Debug Cypress test
npx cypress open --config video=false

# Debug with browser DevTools
npm run test:ui
```

## Maintenance

### Regular Tasks
- Update test dependencies monthly
- Review and update mock data
- Refresh visual regression baselines
- Update accessibility standards
- Performance benchmark updates

### Test Health Monitoring
- Track test execution times
- Monitor flaky test rates
- Review coverage trends
- Accessibility compliance scores
- Performance regression alerts