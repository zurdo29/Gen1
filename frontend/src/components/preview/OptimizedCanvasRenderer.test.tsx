import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '../../test/utils/test-utils'
import { OptimizedCanvasRenderer } from './OptimizedCanvasRenderer'
import { mockLevel } from '../../test/mocks/mockData'

const mockOnTileClick = vi.fn()
const mockOnEntityDrag = vi.fn()

const defaultProps = {
  level: mockLevel,
  isLoading: false,
  onTileClick: mockOnTileClick,
  onEntityDrag: mockOnEntityDrag,
  showGrid: true,
  zoom: 1
}

describe('OptimizedCanvasRenderer', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders canvas element', () => {
    render(<OptimizedCanvasRenderer {...defaultProps} />)
    
    const canvas = screen.getByRole('img', { name: /level preview/i })
    expect(canvas).toBeInTheDocument()
  })

  it('shows loading state when isLoading is true', () => {
    render(<OptimizedCanvasRenderer {...defaultProps} isLoading={true} />)
    
    expect(screen.getByRole('progressbar')).toBeInTheDocument()
    expect(screen.getByText(/generating level/i)).toBeInTheDocument()
  })

  it('handles tile clicks correctly', async () => {
    render(<OptimizedCanvasRenderer {...defaultProps} />)
    
    const canvas = screen.getByRole('img', { name: /level preview/i })
    
    // Simulate click at position (100, 100)
    fireEvent.click(canvas, {
      clientX: 100,
      clientY: 100
    })
    
    expect(mockOnTileClick).toHaveBeenCalled()
  })

  it('handles entity drag operations', async () => {
    render(<OptimizedCanvasRenderer {...defaultProps} />)
    
    const canvas = screen.getByRole('img', { name: /level preview/i })
    
    // Simulate drag operation
    fireEvent.mouseDown(canvas, { clientX: 160, clientY: 160 }) // Entity position
    fireEvent.mouseMove(canvas, { clientX: 200, clientY: 200 })
    fireEvent.mouseUp(canvas, { clientX: 200, clientY: 200 })
    
    expect(mockOnEntityDrag).toHaveBeenCalled()
  })

  it('renders without level data', () => {
    render(<OptimizedCanvasRenderer {...defaultProps} level={null} />)
    
    expect(screen.getByText(/no level data/i)).toBeInTheDocument()
  })

  it('applies zoom correctly', () => {
    render(<OptimizedCanvasRenderer {...defaultProps} zoom={2} />)
    
    const canvas = screen.getByRole('img', { name: /level preview/i })
    expect(canvas).toHaveStyle({ transform: 'scale(2)' })
  })

  it('toggles grid visibility', () => {
    const { rerender } = render(<OptimizedCanvasRenderer {...defaultProps} showGrid={true} />)
    
    // Grid should be visible
    expect(screen.getByLabelText(/toggle grid/i)).toBeChecked()
    
    rerender(<OptimizedCanvasRenderer {...defaultProps} showGrid={false} />)
    
    // Grid should be hidden
    expect(screen.getByLabelText(/toggle grid/i)).not.toBeChecked()
  })

  it('handles keyboard navigation for accessibility', async () => {
    render(<OptimizedCanvasRenderer {...defaultProps} />)
    
    const canvas = screen.getByRole('img', { name: /level preview/i })
    
    // Canvas should be focusable
    expect(canvas).toHaveAttribute('tabIndex', '0')
    
    // Test arrow key navigation
    fireEvent.keyDown(canvas, { key: 'ArrowRight' })
    fireEvent.keyDown(canvas, { key: 'ArrowDown' })
    fireEvent.keyDown(canvas, { key: 'Enter' })
    
    expect(mockOnTileClick).toHaveBeenCalled()
  })
})