/// <reference types="cypress" />

// Custom commands for the level editor
declare global {
  namespace Cypress {
    interface Chainable {
      /**
       * Custom command to configure level generation parameters
       */
      configureLevel(config: {
        width?: number
        height?: number
        generator?: string
        seed?: number
      }): Chainable<Element>

      /**
       * Custom command to wait for level generation to complete
       */
      waitForGeneration(): Chainable<Element>

      /**
       * Custom command to check accessibility violations
       */
      checkA11y(): Chainable<Element>

      /**
       * Custom command to test keyboard navigation
       */
      testKeyboardNavigation(): Chainable<Element>
    }
  }
}

Cypress.Commands.add('configureLevel', (config) => {
  if (config.width) {
    cy.get('[data-testid="terrain-width-input"]').clear().type(config.width.toString())
  }
  
  if (config.height) {
    cy.get('[data-testid="terrain-height-input"]').clear().type(config.height.toString())
  }
  
  if (config.generator) {
    cy.get('[data-testid="terrain-generator-select"]').click()
    cy.get(`[data-value="${config.generator}"]`).click()
  }
  
  if (config.seed) {
    cy.get('[data-testid="terrain-seed-input"]').clear().type(config.seed.toString())
  }
})

Cypress.Commands.add('waitForGeneration', () => {
  cy.get('[data-testid="level-preview"]').should('be.visible')
  cy.get('[data-testid="loading-spinner"]').should('not.exist')
  cy.wait('@generateLevel')
})

Cypress.Commands.add('checkA11y', () => {
  cy.checkA11y(null, null, (violations) => {
    violations.forEach((violation) => {
      cy.log(`Accessibility violation: ${violation.description}`)
      violation.nodes.forEach((node) => {
        cy.log(`Element: ${node.target}`)
      })
    })
  })
})

Cypress.Commands.add('testKeyboardNavigation', () => {
  // Test tab navigation through interactive elements
  cy.get('body').tab()
  cy.focused().should('be.visible')
  
  // Test arrow key navigation in tabs
  cy.get('[role="tab"]').first().focus()
  cy.focused().type('{rightarrow}')
  cy.focused().should('have.attr', 'aria-selected', 'true')
  
  // Test Enter key activation
  cy.focused().type('{enter}')
  cy.get('[role="tabpanel"]').should('be.visible')
})