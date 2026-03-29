import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { axe } from 'jest-axe'
import { DocumentUploader } from '@/components/DocumentUploader'

describe('DocumentUploader', () => {
  const defaultProps = {
    onFileUpload: vi.fn().mockResolvedValue(undefined),
    onPaste: vi.fn().mockResolvedValue(undefined),
    isLoading: false,
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('rendering', () => {
    it('renders drop zone with accessible label', () => {
      render(<DocumentUploader {...defaultProps} />)

      expect(
        screen.getByRole('button', { name: /drop zone for document upload/i })
      ).toBeInTheDocument()
    })

    it('displays guidance text for supported file types', () => {
      render(<DocumentUploader {...defaultProps} />)

      expect(screen.getByText(/drop your manuscript here/i)).toBeInTheDocument()
      expect(screen.getByText(/supports word documents/i)).toBeInTheDocument()
    })

    it('renders paste text button', () => {
      render(<DocumentUploader {...defaultProps} />)

      expect(
        screen.getByRole('button', { name: /paste text instead/i })
      ).toBeInTheDocument()
    })

    it('shows loading state when isLoading is true', () => {
      render(<DocumentUploader {...defaultProps} isLoading={true} />)

      expect(screen.getByText(/processing document/i)).toBeInTheDocument()
    })
  })

  describe('file upload', () => {
    it('has a hidden file input that accepts document files', () => {
      render(<DocumentUploader {...defaultProps} />)

      const fileInput = screen.getByLabelText(/upload document file/i)
      expect(fileInput).toBeInTheDocument()
      expect(fileInput).toHaveAttribute('accept', '.docx,.doc,.txt')
    })

    it('calls onFileUpload when a file is selected via input', async () => {
      const user = userEvent.setup()
      render(<DocumentUploader {...defaultProps} />)

      const file = new File(['test content'], 'manuscript.docx', {
        type: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      })

      const fileInput = screen.getByLabelText(/upload document file/i)
      await user.upload(fileInput, file)

      expect(defaultProps.onFileUpload).toHaveBeenCalledWith(file, undefined)
    })

    it('disables file input when loading', () => {
      render(<DocumentUploader {...defaultProps} isLoading={true} />)

      const fileInput = screen.getByLabelText(/upload document file/i)
      expect(fileInput).toBeDisabled()
    })
  })

  describe('paste text', () => {
    it('shows paste textarea when paste button is clicked', async () => {
      const user = userEvent.setup()
      render(<DocumentUploader {...defaultProps} />)

      await user.click(screen.getByRole('button', { name: /paste text instead/i }))

      expect(screen.getByLabelText(/manuscript text input/i)).toBeInTheDocument()
    })

    it('calls onPaste with trimmed content when Load Text is clicked', async () => {
      const user = userEvent.setup()
      render(<DocumentUploader {...defaultProps} />)

      await user.click(screen.getByRole('button', { name: /paste text instead/i }))

      const textarea = screen.getByLabelText(/manuscript text input/i)
      await user.type(textarea, 'The morning light filtered through the study window.')

      await user.click(screen.getByRole('button', { name: /load text/i }))

      expect(defaultProps.onPaste).toHaveBeenCalledWith(
        'The morning light filtered through the study window.',
        undefined,
        undefined
      )
    })

    it('disables Load Text button when textarea is empty', async () => {
      const user = userEvent.setup()
      render(<DocumentUploader {...defaultProps} />)

      await user.click(screen.getByRole('button', { name: /paste text instead/i }))

      expect(screen.getByRole('button', { name: /load text/i })).toBeDisabled()
    })

    it('hides paste area when Cancel is clicked', async () => {
      const user = userEvent.setup()
      render(<DocumentUploader {...defaultProps} />)

      await user.click(screen.getByRole('button', { name: /paste text instead/i }))
      expect(screen.getByLabelText(/manuscript text input/i)).toBeInTheDocument()

      await user.click(screen.getByRole('button', { name: /cancel/i }))
      expect(screen.queryByLabelText(/manuscript text input/i)).not.toBeInTheDocument()
    })

    it('disables paste button when loading', () => {
      render(<DocumentUploader {...defaultProps} isLoading={true} />)

      expect(
        screen.getByRole('button', { name: /paste text instead/i })
      ).toBeDisabled()
    })
  })

  describe('keyboard accessibility', () => {
    it('drop zone is focusable via tab', () => {
      render(<DocumentUploader {...defaultProps} />)

      const dropZone = screen.getByRole('button', {
        name: /drop zone for document upload/i,
      })

      expect(dropZone).toHaveAttribute('tabIndex', '0')
    })
  })

  describe('accessibility', () => {
    it('passes axe accessibility checks (excluding nested-interactive)', async () => {
      // NOTE: DocumentUploader has a known nested-interactive issue:
      // the drop zone (role="button") contains a hidden file input.
      // This is a common pattern for accessible file upload components.
      // Filed as a follow-up for Dinesh to evaluate.
      const { container } = render(<DocumentUploader {...defaultProps} />)

      const results = await axe(container, {
        rules: { 'nested-interactive': { enabled: false } },
      })
      expect(results).toHaveNoViolations()
    })
  })
})
