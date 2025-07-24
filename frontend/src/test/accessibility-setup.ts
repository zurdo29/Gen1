import { configureAxe } from '@axe-core/react'

// Configure axe for accessibility testing
configureAxe({
  rules: [
    {
      id: 'color-contrast',
      enabled: true
    },
    {
      id: 'keyboard-navigation',
      enabled: true
    },
    {
      id: 'focus-management',
      enabled: true
    }
  ]
})