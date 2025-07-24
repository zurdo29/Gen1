// Import commands.js using ES2015 syntax:
import './commands'

// Alternatively you can use CommonJS syntax:
// require('./commands')

// Add custom assertions
import 'cypress-axe'

// Global before hook
beforeEach(() => {
  // Inject axe for accessibility testing
  cy.injectAxe()
  
  // Set up API intercepts for consistent testing
  cy.intercept('GET', '/api/configuration/presets', { fixture: 'presets.json' }).as('getPresets')
  cy.intercept('GET', '/api/export/formats', { fixture: 'exportFormats.json' }).as('getExportFormats')
  cy.intercept('POST', '/api/generation/generate', { fixture: 'generatedLevel.json' }).as('generateLevel')
  cy.intercept('POST', '/api/generation/validate-config', { fixture: 'validationResult.json' }).as('validateConfig')
})