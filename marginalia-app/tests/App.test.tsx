import { render, screen, waitFor } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import App from '@/App'

// Mock the access code hook so the app renders immediately
vi.mock('@/hooks/useAccessCode', () => ({
  useAccessCode: vi.fn(() => ({
    accessCodeRequired: false,
    isVerified: true,
    isLoading: false,
    error: null,
    submitCode: vi.fn(),
  })),
}))

describe('App', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders the application heading', async () => {
    render(<App />)
    await waitFor(() => {
      expect(screen.getByText('Marginalia')).toBeInTheDocument()
    })
  })
})
