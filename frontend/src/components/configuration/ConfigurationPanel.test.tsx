import { describe, it, expect, vi } from 'vitest'
import { render, screen, _fireEvent, waitFor } from '../../test/utils/test-utils'
import userEvent from '@testing-library/user-event'
import { ConfigurationPanel } from './ConfigurationPanel'
import { mockGenerationConfig } from '../../test/mocks/mockData'

const mockOnChange = vi.fn()
const mockOnValidate = vi.fn()

const defaultProps = {
  config: mockGenerationConfig,
  onChange: mockOnChange,
  onValidate: mockOnValidate,
  isLoading: false,
  validationErrors: []
}

describe('ConfigurationPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders all configuration tabs', () => {
    render(<ConfigurationPanel {...defaultProps} />)
    
    expect(screen.getByRole('tab', { name: /terrain/i })).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: /entities/i })).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: /visual/i })).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: /gameplay/i })).toBeInTheDocument()
  })

  it('switches between tabs correctly', async () => {
    const user = userEvent.setup()
    render(<ConfigurationPanel {...defaultProps} />)
    
    // Default should be terrain tab
    expect(screen.getByRole('tabpanel', { name: /terrain/i })).toBeInTheDocument()
    
    // Switch to entities tab
    await user.click(screen.getByRole('tab', { name: /entities/i }))
    expect(screen.getByRole('tabpanel', { name: /entities/i })).toBeInTheDocument()
  })

  it('calls onChange when configuration is updated', async () => {
    const user = userEvent.setup()
    render(<ConfigurationPanel {...defaultProps} />)
    
    // Find and update a terrain parameter
    const widthInput = screen.getByLabelText(/width/i)
    await user.clear(widthInput)
    await user.type(widthInput, '25')
    
    await waitFor(() => {
      expect(mockOnChange).toHaveBeenCalledWith(
        expect.objectContaining({
          terrain: expect.objectContaining({
            width: 25
          })
        })
      )
    })
  })

  it('displays validation errors correctly', () => {
    const validationErrors = [
      {
        field: 'terrain.width',
        message: 'Width must be between 10 and 100',
        code: 'INVALID_RANGE'
      }
    ]
    
    render(<ConfigurationPanel {...defaultProps} validationErrors={validationErrors} />)
    
    expect(screen.getByText('Width must be between 10 and 100')).toBeInTheDocument()
  })

  it('shows loading state correctly', () => {
    render(<ConfigurationPanel {...defaultProps} isLoading={true} />)
    
    expect(screen.getByRole('progressbar')).toBeInTheDocument()
  })

  it('validates configuration on change', async () => {
    const user = userEvent.setup()
    render(<ConfigurationPanel {...defaultProps} />)
    
    const widthInput = screen.getByLabelText(/width/i)
    await user.clear(widthInput)
    await user.type(widthInput, '150')
    
    await waitFor(() => {
      expect(mockOnValidate).toHaveBeenCalled()
    })
  })
})