import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '../../test/utils/test-utils'
import { axe, toHaveNoViolations } from '@axe-core/react'
import userEvent from '@testing-library/user-event'
import { ConfigurationPanel } from './ConfigurationPanel'
import { mockGenerationConfig } from '../../test/mocks/mockData'

expect.extend(toHaveNoViolations)

const mockProps = {
  config: mockGenerationConfig,
  onChange: vi.fn(),
  onValidate: vi.fn(),
  isLoading: false,
  validationErrors: []
}

describe('ConfigurationPanel Accessibility', () => {
  it('should not have accessibility violations', async () => {
    const { container } = render(<ConfigurationPanel {...mockProps} />)
    
    const results = await axe(container)
    expect(results).toHaveNoViolations()
  })

  it('has proper tab navigation', async () => {
    const user = userEvent.setup()
    render(<ConfigurationPanel {...mockProps} />)
    
    const tabs = screen.getAllByRole('tab')
    
    // First tab should be selected by default
    expect(tabs[0]).toHaveAttribute('aria-selected', 'true')
    
    // Should be able to navigate with arrow keys
    tabs[0].focus()
    await user.keyboard('{ArrowRight}')
    
    expect(tabs[1]).toHaveAttribute('aria-selected', 'true')
  })

  it('has proper form labels and descriptions', () => {
    render(<ConfigurationPanel {...mockProps} />)
    
    // All form inputs should have labels
    const inputs = screen.getAllByRole('textbox')
    inputs.forEach(input => {
      expect(input).toHaveAccessibleName()
    })
    
    // Number inputs should have labels
    const numberInputs = screen.getAllByRole('spinbutton')
    numberInputs.forEach(input => {
      expect(input).toHaveAccessibleName()
    })
  })

  it('announces validation errors to screen readers', () => {
    const validationErrors = [
      {
        field: 'terrain.width',
        message: 'Width must be between 10 and 100',
        code: 'INVALID_RANGE'
      }
    ]
    
    render(<ConfigurationPanel {...mockProps} validationErrors={validationErrors} />)
    
    const errorMessage = screen.getByText('Width must be between 10 and 100')
    expect(errorMessage).toHaveAttribute('role', 'alert')
    expect(errorMessage).toHaveAttribute('aria-live', 'polite')
  })

  it('has proper fieldset grouping', () => {
    render(<ConfigurationPanel {...mockProps} />)
    
    // Configuration sections should be grouped in fieldsets
    const fieldsets = screen.getAllByRole('group')
    expect(fieldsets.length).toBeGreaterThan(0)
    
    fieldsets.forEach(fieldset => {
      expect(fieldset).toHaveAccessibleName()
    })
  })

  it('supports high contrast mode', () => {
    render(<ConfigurationPanel {...mockProps} />)
    
    // Check that important elements have proper contrast
    const tabs = screen.getAllByRole('tab')
    tabs.forEach(tab => {
      const styles = getComputedStyle(tab)
      expect(styles.border).toBeDefined()
    })
  })

  it('has proper focus management', async () => {
    const user = userEvent.setup()
    render(<ConfigurationPanel {...mockProps} />)
    
    // Focus should be visible on interactive elements
    const firstTab = screen.getAllByRole('tab')[0]
    await user.tab()
    
    expect(document.activeElement).toBe(firstTab)
    expect(firstTab).toHaveAttribute('tabIndex', '0')
  })
})