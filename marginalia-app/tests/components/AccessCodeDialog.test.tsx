import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { axe } from 'jest-axe'
import { AccessCodeDialog } from '@/components/AccessCodeDialog'

describe('AccessCodeDialog', () => {
  const defaultProps = {
    open: true,
    onSubmit: vi.fn(),
    isLoading: false,
    error: null,
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('rendering', () => {
    it('renders dialog title when open', () => {
      render(<AccessCodeDialog {...defaultProps} />)

      expect(screen.getByText('Access Code Required')).toBeInTheDocument()
    })

    it('renders description text', () => {
      render(<AccessCodeDialog {...defaultProps} />)

      expect(
        screen.getByText('This application is protected. Enter the access code to continue.')
      ).toBeInTheDocument()
    })

    it('renders access code input', () => {
      render(<AccessCodeDialog {...defaultProps} />)

      expect(screen.getByTestId('access-code-input')).toBeInTheDocument()
    })

    it('renders continue button', () => {
      render(<AccessCodeDialog {...defaultProps} />)

      expect(screen.getByRole('button', { name: /continue/i })).toBeInTheDocument()
    })

    it('does not render when closed', () => {
      render(<AccessCodeDialog {...defaultProps} open={false} />)

      expect(screen.queryByText('Access Code Required')).not.toBeInTheDocument()
    })
  })

  describe('interactions', () => {
    it('calls onSubmit with entered code when form is submitted', async () => {
      const user = userEvent.setup()
      const onSubmit = vi.fn().mockResolvedValue(undefined)
      render(<AccessCodeDialog {...defaultProps} onSubmit={onSubmit} />)

      const input = screen.getByTestId('access-code-input')
      await user.type(input, 'my-access-code')
      await user.click(screen.getByRole('button', { name: /continue/i }))

      expect(onSubmit).toHaveBeenCalledWith('my-access-code')
    })

    it('disables continue button when input is empty', () => {
      render(<AccessCodeDialog {...defaultProps} />)

      expect(screen.getByRole('button', { name: /continue/i })).toBeDisabled()
    })

    it('enables continue button when input has text', async () => {
      const user = userEvent.setup()
      render(<AccessCodeDialog {...defaultProps} />)

      await user.type(screen.getByTestId('access-code-input'), 'some-code')

      expect(screen.getByRole('button', { name: /continue/i })).toBeEnabled()
    })

    it('trims whitespace from submitted code', async () => {
      const user = userEvent.setup()
      const onSubmit = vi.fn().mockResolvedValue(undefined)
      render(<AccessCodeDialog {...defaultProps} onSubmit={onSubmit} />)

      await user.type(screen.getByTestId('access-code-input'), '  my-code  ')
      await user.click(screen.getByRole('button', { name: /continue/i }))

      expect(onSubmit).toHaveBeenCalledWith('my-code')
    })
  })

  describe('loading state', () => {
    it('disables input when loading', () => {
      render(<AccessCodeDialog {...defaultProps} isLoading={true} />)

      expect(screen.getByTestId('access-code-input')).toBeDisabled()
    })

    it('disables button when loading', async () => {
      const user = userEvent.setup()
      render(<AccessCodeDialog {...defaultProps} isLoading={false} />)

      await user.type(screen.getByTestId('access-code-input'), 'code')

      const { rerender } = render(<AccessCodeDialog {...defaultProps} isLoading={true} />)
      void rerender

      expect(screen.getAllByRole('button', { name: /continue/i })[0]).toBeDisabled()
    })
  })

  describe('error display', () => {
    it('shows error message when error is set', () => {
      render(<AccessCodeDialog {...defaultProps} error="Invalid access code" />)

      expect(screen.getByTestId('access-code-error')).toHaveTextContent('Invalid access code')
    })

    it('does not show error when error is null', () => {
      render(<AccessCodeDialog {...defaultProps} error={null} />)

      expect(screen.queryByTestId('access-code-error')).not.toBeInTheDocument()
    })
  })

  describe('accessibility', () => {
    it('has no accessibility violations', async () => {
      const { container } = render(<AccessCodeDialog {...defaultProps} />)

      const results = await axe(container)

      expect(results).toHaveNoViolations()
    })

    it('input has associated label', () => {
      render(<AccessCodeDialog {...defaultProps} />)

      expect(screen.getByLabelText('Access Code')).toBeInTheDocument()
    })

    it('input uses password type for masking', () => {
      render(<AccessCodeDialog {...defaultProps} />)

      expect(screen.getByTestId('access-code-input')).toHaveAttribute('type', 'password')
    })
  })
})
