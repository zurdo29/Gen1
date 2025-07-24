describe('Level Generation Workflow', () => {
  beforeEach(() => {
    cy.visit('/')
  })

  it('should generate a level with default parameters', () => {
    // Check that the app loads correctly
    cy.get('[data-testid="app-title"]').should('contain', 'Procedural Level Editor')
    
    // Generate level with default settings
    cy.get('[data-testid="generate-button"]').click()
    
    // Wait for generation to complete
    cy.waitForGeneration()
    
    // Verify level preview is displayed
    cy.get('[data-testid="level-preview"]').should('be.visible')
    cy.get('[data-testid="level-canvas"]').should('be.visible')
    
    // Verify level metadata is shown
    cy.get('[data-testid="level-metadata"]').should('contain', 'Generated')
    cy.get('[data-testid="generation-time"]').should('be.visible')
  })

  it('should configure terrain parameters and generate level', () => {
    // Navigate to terrain configuration
    cy.get('[role="tab"][aria-label="Terrain"]').click()
    
    // Configure terrain parameters
    cy.configureLevel({
      width: 25,
      height: 25,
      generator: 'cellular-automata',
      seed: 54321
    })
    
    // Generate level
    cy.get('[data-testid="generate-button"]').click()
    cy.waitForGeneration()
    
    // Verify the level was generated with correct dimensions
    cy.get('[data-testid="level-info"]').should('contain', '25x25')
    cy.get('[data-testid="level-preview"]').should('be.visible')
  })

  it('should validate configuration in real-time', () => {
    // Navigate to terrain configuration
    cy.get('[role="tab"][aria-label="Terrain"]').click()
    
    // Enter invalid width
    cy.get('[data-testid="terrain-width-input"]').clear().type('5')
    
    // Should show validation error
    cy.get('[data-testid="validation-error"]').should('contain', 'Width must be between 10 and 100')
    cy.get('[data-testid="generate-button"]').should('be.disabled')
    
    // Fix the validation error
    cy.get('[data-testid="terrain-width-input"]').clear().type('20')
    
    // Error should disappear and generate button should be enabled
    cy.get('[data-testid="validation-error"]').should('not.exist')
    cy.get('[data-testid="generate-button"]').should('not.be.disabled')
  })

  it('should handle generation errors gracefully', () => {
    // Mock a generation error
    cy.intercept('POST', '/api/generation/generate', {
      statusCode: 400,
      body: { error: 'Generation failed', details: 'Invalid configuration' }
    }).as('generateError')
    
    // Attempt to generate
    cy.get('[data-testid="generate-button"]').click()
    cy.wait('@generateError')
    
    // Should show error message
    cy.get('[data-testid="error-dialog"]').should('be.visible')
    cy.get('[data-testid="error-message"]').should('contain', 'Generation failed')
    
    // Should be able to dismiss error
    cy.get('[data-testid="error-dismiss"]').click()
    cy.get('[data-testid="error-dialog"]').should('not.exist')
  })

  it('should show loading state during generation', () => {
    // Mock slow generation
    cy.intercept('POST', '/api/generation/generate', (req) => {
      req.reply((res) => {
        res.delay(2000)
        res.send({ fixture: 'generatedLevel.json' })
      })
    }).as('slowGeneration')
    
    // Start generation
    cy.get('[data-testid="generate-button"]').click()
    
    // Should show loading state
    cy.get('[data-testid="loading-spinner"]').should('be.visible')
    cy.get('[data-testid="generate-button"]').should('be.disabled')
    cy.get('[data-testid="progress-indicator"]').should('be.visible')
    
    // Wait for completion
    cy.wait('@slowGeneration')
    
    // Loading state should disappear
    cy.get('[data-testid="loading-spinner"]').should('not.exist')
    cy.get('[data-testid="generate-button"]').should('not.be.disabled')
  })
})