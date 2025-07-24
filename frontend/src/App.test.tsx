import { render, screen } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ThemeProvider, createTheme } from '@mui/material/styles'
import App from './App'

const theme = createTheme()

const renderWithProviders = (component: React.ReactElement) => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        {component}
      </ThemeProvider>
    </QueryClientProvider>
  )
}

test('renders procedural level editor title', () => {
  renderWithProviders(<App />)
  const titleElement = screen.getByRole('heading', { name: /Procedural Level Editor/i, level: 1 })
  expect(titleElement).toBeInTheDocument()
})