describe('Level Editing Features', () => {
  beforeEach(() => {
    cy.visit('/')
    // Generate a level first
    cy.get('[data-testid="generate-button"]').click()
    cy.waitForGeneration()
  })

  it('should allow terrain tile editing', () => {
    // Click on a terrain tile
    cy.get('[data-testid="level-canvas"]').click(100, 100)
    
    // Should show terrain edit menu
    cy.get('[data-testid="terrain-edit-menu"]').should('be.visible')
    
    // Select different terrain type
    cy.get('[data-testid="terrain-type-water"]').click()
    
    // Should update the tile
    cy.get('[data-testid="level-canvas"]').should('be.visible')
    
    // Should show undo option
    cy.get('[data-testid="undo-button"]').should('not.be.disabled')
  })

  it('should support entity drag and drop', () => {
    // Find an entity on the canvas
    cy.get('[data-testid="level-canvas"]')
      .trigger('mousedown', 160, 160) // Entity position
      .trigger('mousemove', 200, 200) // Drag to new position
      .trigger('mouseup')
    
    // Should show entity move confirmation
    cy.get('[data-testid="entity-moved-notification"]').should('be.visible')
    
    // Should update entity position
    cy.get('[data-testid="entity-info"]').should('contain', 'Position: (6, 6)')
  })

  it('should validate entity placement rules', () => {
    // Try to place entity in invalid location
    cy.get('[data-testid="add-entity-button"]').click()
    cy.get('[data-testid="entity-type-enemy"]').click()
    
    // Click on water tile (invalid for enemy)
    cy.get('[data-testid="level-canvas"]').click(50, 50)
    
    // Should show validation warning
    cy.get('[data-testid="placement-warning"]').should('contain', 'Cannot place enemy on water')
    
    // Should not place the entity
    cy.get('[data-testid="entity-count"]').should('not.contain', 'Enemies: 2')
  })

  it('should support undo/redo operations', () => {
    // Make an edit
    cy.get('[data-testid="level-canvas"]').click(100, 100)
    cy.get('[data-testid="terrain-type-water"]').click()
    
    // Undo the edit
    cy.get('[data-testid="undo-button"]').click()
    
    // Should revert the change
    cy.get('[data-testid="undo-button"]').should('be.disabled')
    
    // Redo the edit
    cy.get('[data-testid="redo-button"]').click()
    
    // Should restore the change
    cy.get('[data-testid="undo-button"]').should('not.be.disabled')
  })

  it('should save manual edits with generation parameters', () => {
    // Make some manual edits
    cy.get('[data-testid="level-canvas"]').click(100, 100)
    cy.get('[data-testid="terrain-type-water"]').click()
    
    // Save the level
    cy.get('[data-testid="save-level-button"]').click()
    cy.get('[data-testid="level-name-input"]').type('My Custom Level')
    cy.get('[data-testid="save-confirm-button"]').click()
    
    // Should show save confirmation
    cy.get('[data-testid="save-success-notification"]').should('be.visible')
    
    // Should preserve both generation config and manual edits
    cy.get('[data-testid="level-info"]').should('contain', 'Manual edits: 1')
  })

  it('should allow reverting to original generated state', () => {
    // Make manual edits
    cy.get('[data-testid="level-canvas"]').click(100, 100)
    cy.get('[data-testid="terrain-type-water"]').click()
    
    // Should show revert option
    cy.get('[data-testid="revert-button"]').should('not.be.disabled')
    
    // Revert to original
    cy.get('[data-testid="revert-button"]').click()
    cy.get('[data-testid="revert-confirm-button"]').click()
    
    // Should restore original state
    cy.get('[data-testid="revert-button"]').should('be.disabled')
    cy.get('[data-testid="level-info"]').should('not.contain', 'Manual edits')
  })

  it('should support grid toggle and zoom controls', () => {
    // Toggle grid
    cy.get('[data-testid="toggle-grid-button"]').click()
    cy.get('[data-testid="level-canvas"]').should('have.class', 'no-grid')
    
    // Toggle back
    cy.get('[data-testid="toggle-grid-button"]').click()
    cy.get('[data-testid="level-canvas"]').should('not.have.class', 'no-grid')
    
    // Test zoom controls
    cy.get('[data-testid="zoom-in-button"]').click()
    cy.get('[data-testid="zoom-level"]').should('contain', '150%')
    
    cy.get('[data-testid="zoom-out-button"]').click()
    cy.get('[data-testid="zoom-level"]').should('contain', '100%')
  })
})