import { describe, it, expect } from 'vitest'
import { render, screen } from '../../test/utils/test-utils'
import { axe, toHaveNoViolations } from '@axe-core/react'
import { AppShell } from './AppShell'

expect.extend(toHaveNoViolations)

describe('AppShell Accessibility', () => {
  it('should not have accessibility violations', async () => {
    const { container } = render(
      <AppShell>
        <div>Test content</div>
      </AppShell>
    )
    
    const results = await axe(container)
    expect(results).toHaveNoViolations()
  })

  it('has proper heading hierarchy', () => {
    render(
      <AppShell>
        <div>Test content</div>
      </AppShell>
    )
    
    // Main heading should be h1
    const mainHeading = screen.getByRole('heading', { level: 1 })
    expect(mainHeading).toBeInTheDocument()
    expect(mainHeading).toHaveTextContent(/procedural level editor/i)
  })

  it('has proper navigation landmarks', () => {
    render(
      <AppShell>
        <div>Test content</div>
      </AppShell>
    )
    
    expect(screen.getByRole('navigation')).toBeInTheDocument()
    expect(screen.getByRole('main')).toBeInTheDocument()
  })

  it('supports keyboard navigation', async () => {
    render(
      <AppShell>
        <div>Test content</div>
      </AppShell>
    )
    
    // All interactive elements should be focusable
    const interactiveElements = screen.getAllByRole('button')
    interactiveElements.forEach(element => {
      expect(element).toHaveAttribute('tabIndex')
    })
  })

  it('has proper ARIA labels', () => {
    render(
      <AppShell>
        <div>Test content</div>
      </AppShell>
    )
    
    const navigation = screen.getByRole('navigation')
    expect(navigation).toHaveAttribute('aria-label')
    
    const main = screen.getByRole('main')
    expect(main).toHaveAttribute('aria-label')
  })

  it('announces page changes to screen readers', () => {
    render(
      <AppShell>
        <div>Test content</div>
      </AppShell>
    )
    
    // Should have live region for announcements
    expect(screen.getByRole('status')).toBeInTheDocument()
  })
})