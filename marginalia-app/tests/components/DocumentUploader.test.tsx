import { fireEvent, render, screen, waitFor } from '@testing-library/react'
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
    it('renders a native browse button', () => {
      render(<DocumentUploader {...defaultProps} />)

      expect(
        screen.getByRole('button', { name: /browse manuscript/i })
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
      expect(fileInput).toHaveAttribute('accept', '.docx')
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

    it.each(['manuscript.doc', 'manuscript.txt'])('rejects unsupported %s for selection and drop', async (name) => {
      const { container } = render(<DocumentUploader {...defaultProps} />)
      const file = new File(['text'], name)
      const input = screen.getByLabelText(/upload document file/i)
      fireEvent.change(input, { target: { files: [file] } })
      expect(screen.getByRole('alert')).toHaveTextContent('Choose a Word document (.docx).')
      expect(input).toHaveFocus()
      expect(input).toHaveAttribute('aria-invalid', 'true')
      const dropZone = container.querySelector('[data-slot="card"]')
      if (!dropZone) throw new Error('Upload surface missing')
      fireEvent.drop(dropZone, { dataTransfer: { files: [file] } })
      expect(defaultProps.onFileUpload).not.toHaveBeenCalled()
    })

    it.each([50 * 1024 * 1024, 50 * 1024 * 1024 + 1])('validates the 50 MB boundary (%i bytes) for selection and drop', async (size) => {
      const { container } = render(<DocumentUploader {...defaultProps} />)
      const file = new File(['text'], 'manuscript.docx')
      Object.defineProperty(file, 'size', { value: size })
      fireEvent.change(screen.getByLabelText(/upload document file/i), { target: { files: [file] } })
      const dropZone = container.querySelector('[data-slot="card"]')
      if (!dropZone) throw new Error('Upload surface missing')
      fireEvent.drop(dropZone, { dataTransfer: { files: [file] } })
      if (size === 50 * 1024 * 1024) {
        expect(defaultProps.onFileUpload).toHaveBeenCalledTimes(2)
      } else {
        expect(defaultProps.onFileUpload).not.toHaveBeenCalled()
        expect(screen.getByRole('alert')).toHaveTextContent('50 MB')
      }
    })

    it('does not process drops while loading', () => {
      const { container } = render(<DocumentUploader {...defaultProps} isLoading />)
      const dropZone = container.querySelector('[data-slot="card"]')
      if (!dropZone) throw new Error('Upload surface missing')
      fireEvent.drop(dropZone, { dataTransfer: { files: [new File(['text'], 'manuscript.docx')] } })
      expect(defaultProps.onFileUpload).not.toHaveBeenCalled()
    })

    it('reports upload failures', async () => {
      const onFileUpload = vi.fn().mockRejectedValue(new Error('Upload unavailable. Please retry.'))
      render(<DocumentUploader {...defaultProps} onFileUpload={onFileUpload} />)
      fireEvent.change(screen.getByLabelText(/upload document file/i), {
        target: { files: [new File(['text'], 'manuscript.docx')] },
      })
      expect(await screen.findByRole('alert')).toHaveTextContent('Upload unavailable. Please retry.')
    })
  })

  describe('paste text', () => {
    it('shows paste textarea when paste button is clicked', async () => {
      const user = userEvent.setup()
      render(<DocumentUploader {...defaultProps} />)

      await user.click(screen.getByRole('button', { name: /paste text instead/i }))

      expect(screen.getByLabelText(/paste your text/i)).toHaveFocus()
    })

    it('calls onPaste with trimmed content when Load Text is clicked', async () => {
      const user = userEvent.setup()
      render(<DocumentUploader {...defaultProps} />)

      await user.click(screen.getByRole('button', { name: /paste text instead/i }))

      const textarea = screen.getByLabelText(/paste your text/i)
      await user.type(textarea, 'The morning light filtered through the study window.')

      await user.click(screen.getByRole('button', { name: /load text/i }))

      expect(defaultProps.onPaste).toHaveBeenCalledWith(
        'The morning light filtered through the study window.',
        undefined,
        undefined
      )
    })

    it('explains empty paste submission and focuses the field', async () => {
      const user = userEvent.setup()
      render(<DocumentUploader {...defaultProps} />)

      await user.click(screen.getByRole('button', { name: /paste text instead/i }))

      await user.click(screen.getByRole('button', { name: /load text/i }))
      expect(screen.getByRole('alert')).toHaveTextContent('Paste manuscript text before loading it.')
      expect(screen.getByLabelText(/paste your text/i)).toHaveFocus()
      expect(screen.getByLabelText(/paste your text/i)).toHaveAttribute('aria-describedby', 'paste-error')
      expect(defaultProps.onPaste).not.toHaveBeenCalled()
    })

    it('hides paste area when Cancel is clicked', async () => {
      const user = userEvent.setup()
      render(<DocumentUploader {...defaultProps} />)

      await user.click(screen.getByRole('button', { name: /paste text instead/i }))
      expect(screen.getByLabelText(/paste your text/i)).toBeInTheDocument()

      await user.click(screen.getByRole('button', { name: /cancel/i }))
      expect(screen.queryByLabelText(/paste your text/i)).not.toBeInTheDocument()
      expect(screen.getByRole('button', { name: /paste text instead/i })).toHaveFocus()
    })

    it('preserves paragraphs, line breaks, spacing and long sentences in submitted text', async () => {
      const user = userEvent.setup()
      render(<DocumentUploader {...defaultProps} />)
      await user.click(screen.getByRole('button', { name: /paste text instead/i }))
      const content = `First paragraph.\nA deliberate break.\n\n${'A long sentence with  internal spaces. '.repeat(20)}End.`
      fireEvent.change(screen.getByLabelText(/paste your text/i), { target: { value: content } })
      await user.click(screen.getByRole('button', { name: /load text/i }))
      expect(defaultProps.onPaste).toHaveBeenCalledWith(content, undefined, undefined)
    })

    it('retains the draft and exposes an associated error when loading fails', async () => {
      const user = userEvent.setup()
      render(<DocumentUploader {...defaultProps} onPaste={vi.fn().mockRejectedValue(new Error('Please retry loading.'))} />)
      await user.click(screen.getByRole('button', { name: /paste text instead/i }))
      const input = screen.getByLabelText(/paste your text/i)
      await user.type(input, 'Keep this draft.')
      await user.click(screen.getByRole('button', { name: /load text/i }))
      await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('Please retry loading.'))
      expect(input).toHaveValue('Keep this draft.')
      expect(input).toHaveAttribute('aria-invalid', 'true')
    })

    it('disables paste button when loading', () => {
      render(<DocumentUploader {...defaultProps} isLoading={true} />)

      expect(
        screen.getByRole('button', { name: /paste text instead/i })
      ).toBeDisabled()
    })
  })

  describe('keyboard accessibility', () => {
    it('browse action is keyboard operable', async () => {
      const user = userEvent.setup()
      render(<DocumentUploader {...defaultProps} />)
      const inputClick = vi.spyOn(screen.getByLabelText(/upload document file/i), 'click')
      const browse = screen.getByRole('button', { name: /browse manuscript/i })
      browse.focus()
      await user.keyboard('{Enter}')
      expect(inputClick).toHaveBeenCalledOnce()
    })
  })

  describe('accessibility', () => {
    it('passes axe accessibility checks', async () => {
      const { container } = render(<DocumentUploader {...defaultProps} />)

      const results = await axe(container)
      expect(results).toHaveNoViolations()
    })
  })
})
