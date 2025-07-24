describe('Accessibility Tests', () => {
  beforeEach(() => {
    cy.visit('/')
  })

  it('should have no accessibility violations on main page', () => {
    cy.checkA11y()
  })

  it('should support keyboard navigation', () => {
    cy.testKeyboardNavigation()
  })

  it('should have proper focus management', () => {
    // Test focus trap in dialogs
    cy.get('[data-testid="export-button"]').click()
    cy.get('[data-testid="export-dialog"]').should('be.visible')
    
    // Focus should be trapped within dialog
    cy.get('[data-testid="export-format-select"]').should('be.focused')
    
    // Tab through dialog elements
    cy.focused().tab()
    cy.focused().should('have.attr', 'data-testid', 'export-options-button')
    
    // Escape should close dialog and restore focus
    cy.focused().type('{esc}')
    cy.get('[data-testid="export-dialog"]').should('not.exist')
    cy.get('[data-testid="export-button"]').should('be.focused')
  })

  it('should announce dynamic content changes', () => {
    // Generate a level
    cy.get('[data-testid="generate-button"]').click()
    
    // Should announce generation completion
    cy.get('[role="status"]').should('contain', 'Level generation completed')
    
    // Make an edit
    cy.waitForGeneration()
    cy.get('[data-testid="level-canvas"]').click(100, 100)
    cy.get('[data-testid="terrain-type-water"]').click()
    
    // Should announce the edit
    cy.get('[role="status"]').should('contain', 'Terrain tile updated')
  })

  it('should have proper form labels and error messages', () => {
    // Navigate to configuration
    cy.get('[role="tab"][aria-label="Terrain"]').click()
    
    // All inputs should have labels
    cy.get('[data-testid="terrain-width-input"]').should('have.attr', 'aria-label')
    cy.get('[data-testid="terrain-height-input"]').should('have.attr', 'aria-label')
    
    // Test error message association
    cy.get('[data-testid="terrain-width-input"]').clear().type('5')
    
    // Error should be associated with input
    cy.get('[data-testid="terrain-width-input"]')
      .should('have.attr', 'aria-invalid', 'true')
      .should('have.attr', 'aria-describedby')
    
    const describedBy = cy.get('[data-testid="terrain-width-input"]').invoke('attr', 'aria-describedby')
    describedBy.then((id) => {
      cy.get(`#${id}`).should('contain', 'Width must be between 10 and 100')
    })
  })

  it('should support screen reader navigation of level preview', () => {
    // Generate a level first
    cy.get('[data-testid="generate-button"]').click()
    cy.waitForGeneration()
    
    // Canvas should have proper role and label
    cy.get('[data-testid="level-canvas"]')
      .should('have.attr', 'role', 'img')
      .should('have.attr', 'aria-label', 'Level preview')
    
    // Should have keyboard navigation instructions
    cy.get('[data-testid="canvas-instructions"]')
      .should('contain', 'Use arrow keys to navigate')
      .should('contain', 'Press Enter to edit tile')
  })

  it('should have high contrast support', () => {
    // Test with high contrast mode
    cy.get('body').invoke('addClass', 'high-contrast')
    
    // Important elements should have sufficient contrast
    cy.get('[data-testid="generate-button"]').should('be.visible')
    cy.get('[role="tab"]').should('be.visible')
    
    // Check that focus indicators are visible
    cy.get('[data-testid="generate-button"]').focus()
    cy.get('[data-testid="generate-button"]').should('have.css', 'outline')
  })

  it('should support reduced motion preferences', () => {
    // Mock reduced motion preference
    cy.window().then((win) => {
      Object.defineProperty(win, 'matchMedia', {
        writable: true,
        value: cy.stub().returns({
          matches: true,
          media: '(prefers-reduced-motion: reduce)',
          onchange: null,
          addListener: cy.stub(),
          removeListener: cy.stub(),
          addEventListener: cy.stub(),
          removeEventListener: cy.stub(),
          dispatchEvent: cy.stub(),
        }),
      })
    })
    
    cy.reload()
    
    // Animations should be disabled
    cy.get('[data-testid="loading-spinner"]').should('have.css', 'animation-duration', '0s')
  })

  it('should have proper heading hierarchy', () => {
    // Check heading structure
    cy.get('h1').should('have.length', 1)
    cy.get('h1').should('contain', 'Procedural Level Editor')
    
    // Configuration sections should use proper heading levels
    cy.get('[role="tab"][aria-label="Terrain"]').click()
    cy.get('h2').should('contain', 'Terrain Configuration')
    
    // Subsections should use h3
    cy.get('h3').should('exist')
  })
})