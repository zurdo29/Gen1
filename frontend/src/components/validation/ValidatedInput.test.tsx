import { describe, it, expect, vi } from 'vitest'
import { render, screen, waitFor } from '../../test/utils/test-utils'
import userEvent from '@testing-library/user-event'
import { ValidatedInput } from './ValidatedInput'

const mockOnChange = vi.fn()
const mockOnValidate = vi.fn()

const defaultProps = {
  label: 'Test Input',
  value: '',
  onChange: mockOnChange,
  onValidate: mockOnValidate,
  type: 'text' as const,
  fieldPath: 'test.field'
}

describe('ValidatedInput', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders input with label', () => {
    render(<ValidatedInput {...defaultProps} />)
    
    expect(screen.getByLabelText('Test Input')).toBeInTheDocument()
  })

  it('calls onChange when value changes', async () => {
    const user = userEvent.setup()
    render(<ValidatedInput {...defaultProps} />)
    
    const input = screen.getByLabelText('Test Input')
    await user.type(input, 'test value')
    
    expect(mockOnChange).toHaveBeenCalledWith('test value')
  })

  it('validates input on blur', async () => {
    const user = userEvent.setup()
    render(<ValidatedInput {...defaultProps} />)
    
    const input = screen.getByLabelText('Test Input')
    await user.type(input, 'test')
    await user.tab() // Trigger blur
    
    expect(mockOnValidate).toHaveBeenCalledWith('test')
  })

  it('displays validation error', () => {
    const error = {
      field: 'test',
      message: 'This field is required',
      code: 'REQUIRED'
    }
    
    render(<ValidatedInput {...defaultProps} error={error} />)
    
    expect(screen.getByText('This field is required')).toBeInTheDocument()
    expect(screen.getByLabelText('Test Input')).toHaveAttribute('aria-invalid', 'true')
  })

  it('shows success state when valid', () => {
    render(<ValidatedInput {...defaultProps} isValid={true} />)
    
    expect(screen.getByLabelText('Test Input')).toHaveAttribute('aria-invalid', 'false')
  })

  it('handles number input type', async () => {
    const user = userEvent.setup()
    render(<ValidatedInput {...defaultProps} type="number" min={0} max={100} />)
    
    const input = screen.getByLabelText('Test Input')
    expect(input).toHaveAttribute('type', 'number')
    expect(input).toHaveAttribute('min', '0')
    expect(input).toHaveAttribute('max', '100')
    
    await user.type(input, '50')
    expect(mockOnChange).toHaveBeenCalledWith(50)
  })

  it('handles required field validation', () => {
    render(<ValidatedInput {...defaultProps} required={true} />)
    
    const input = screen.getByLabelText('Test Input')
    expect(input).toHaveAttribute('required')
    expect(input).toHaveAttribute('aria-required', 'true')
  })

  it('debounces validation calls', async () => {
    const user = userEvent.setup()
    render(<ValidatedInput {...defaultProps} debounceMs={300} />)
    
    const input = screen.getByLabelText('Test Input')
    
    // Type multiple characters quickly
    await user.type(input, 'test')
    
    // Validation should not be called immediately
    expect(mockOnValidate).not.toHaveBeenCalled()
    
    // Wait for debounce
    await waitFor(() => {
      expect(mockOnValidate).toHaveBeenCalledWith('test')
    }, { timeout: 500 })
  })

  it('handles disabled state', () => {
    render(<ValidatedInput {...defaultProps} disabled={true} />)
    
    const input = screen.getByLabelText('Test Input')
    expect(input).toBeDisabled()
  })

  it('supports custom placeholder', () => {
    render(<ValidatedInput {...defaultProps} placeholder="Enter value here" />)
    
    const input = screen.getByLabelText('Test Input')
    expect(input).toHaveAttribute('placeholder', 'Enter value here')
  })
})