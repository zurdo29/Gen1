import { describe, it, expect } from 'vitest'
import { render } from '../../test/utils/test-utils'
import { LevelRenderer } from './LevelRenderer'
import { mockLevel } from '../../test/mocks/mockData'

// Visual regression tests for level rendering
describe('LevelRenderer Visual Tests', () => {
  it('renders level preview consistently', async () => {
    const { container } = render(
      <LevelRenderer 
        level={mockLevel}
        isLoading={false}
        onTileClick={() => {}}
        onEntityDrag={() => {}}
        showGrid={true}
        zoom={1}
      />
    )
    
    // Wait for canvas to render
    await new Promise(resolve => setTimeout(resolve, 100))
    
    // Take snapshot for visual regression testing
    expect(container.firstChild).toMatchSnapshot('level-renderer-default.png')
  })

  it('renders level with different zoom levels consistently', async () => {
    const { container, rerender } = render(
      <LevelRenderer 
        level={mockLevel}
        isLoading={false}
        onTileClick={() => {}}
        onEntityDrag={() => {}}
        showGrid={true}
        zoom={1}
      />
    )
    
    await new Promise(resolve => setTimeout(resolve, 100))
    expect(container.firstChild).toMatchSnapshot('level-renderer-zoom-100.png')
    
    rerender(
      <LevelRenderer 
        level={mockLevel}
        isLoading={false}
        onTileClick={() => {}}
        onEntityDrag={() => {}}
        showGrid={true}
        zoom={2}
      />
    )
    
    await new Promise(resolve => setTimeout(resolve, 100))
    expect(container.firstChild).toMatchSnapshot('level-renderer-zoom-200.png')
  })

  it('renders level without grid consistently', async () => {
    const { container } = render(
      <LevelRenderer 
        level={mockLevel}
        isLoading={false}
        onTileClick={() => {}}
        onEntityDrag={() => {}}
        showGrid={false}
        zoom={1}
      />
    )
    
    await new Promise(resolve => setTimeout(resolve, 100))
    expect(container.firstChild).toMatchSnapshot('level-renderer-no-grid.png')
  })

  it('renders loading state consistently', () => {
    const { container } = render(
      <LevelRenderer 
        level={null}
        isLoading={true}
        onTileClick={() => {}}
        onEntityDrag={() => {}}
        showGrid={true}
        zoom={1}
      />
    )
    
    expect(container.firstChild).toMatchSnapshot('level-renderer-loading.png')
  })

  it('renders empty state consistently', () => {
    const { container } = render(
      <LevelRenderer 
        level={null}
        isLoading={false}
        onTileClick={() => {}}
        onEntityDrag={() => {}}
        showGrid={true}
        zoom={1}
      />
    )
    
    expect(container.firstChild).toMatchSnapshot('level-renderer-empty.png')
  })
})