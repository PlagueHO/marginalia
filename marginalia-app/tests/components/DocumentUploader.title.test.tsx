import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { DocumentUploader } from '@/components/DocumentUploader'

/**
 * Tests for the optional title field on DocumentUploader.
 *
 * Per Richard's API design:
 * - Upload accepts an optional `title` form field
 * - Paste accepts an optional `title` in the JSON body
 * - When not provided, the backend generates a default title
 *
 * These tests will FAIL at runtime until Dinesh adds the title input
 * to the DocumentUploader component. They compile against the existing
 * component — failures are getByLabelText throwing because the element
 * doesn't exist yet.
 */
describe('DocumentUploader - Title Support', () => {
  const defaultProps = {
    onFileUpload: vi.fn().mockResolvedValue(undefined),
    onPaste: vi.fn().mockResolvedValue(undefined),
    isLoading: false,
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders a title input field', () => {
    render(<DocumentUploader {...defaultProps} />)

    expect(screen.getByLabelText(/manuscript title/i)).toBeInTheDocument()
  })

  it('title input is optional - not required', () => {
    render(<DocumentUploader {...defaultProps} />)

    const titleInput = screen.getByLabelText(/manuscript title/i)
    expect(titleInput).not.toBeRequired()
  })

  it('passes title to onFileUpload when file is uploaded with title', async () => {
    const user = userEvent.setup()
    render(<DocumentUploader {...defaultProps} />)

    // Fill in the title
    const titleInput = screen.getByLabelText(/manuscript title/i)
    await user.type(titleInput, 'Chapter 1: The Beginning')

    // Upload a file
    const file = new File(['content'], 'manuscript.docx', {
      type: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    })
    const fileInput = screen.getByLabelText(/upload document file/i)
    await user.upload(fileInput, file)

    expect(defaultProps.onFileUpload).toHaveBeenCalledWith(
      expect.any(File),
      'Chapter 1: The Beginning'
    )
  })

  it('passes title to onPaste when text is pasted with title', async () => {
    const user = userEvent.setup()
    render(<DocumentUploader {...defaultProps} />)

    // Fill in the title
    const titleInput = screen.getByLabelText(/manuscript title/i)
    await user.type(titleInput, 'My Research Notes')

    // Switch to paste mode, enter text, and submit
    await user.click(screen.getByRole('button', { name: /paste text instead/i }))

    const textarea = screen.getByLabelText(/manuscript text input/i)
    await user.type(textarea, 'Some pasted manuscript content.')

    await user.click(screen.getByRole('button', { name: /load text/i }))

    expect(defaultProps.onPaste).toHaveBeenCalledWith(
      'Some pasted manuscript content.',
      undefined,
      'My Research Notes'
    )
  })

  it('file upload works without title - title is optional', async () => {
    const user = userEvent.setup()
    render(<DocumentUploader {...defaultProps} />)

    // Upload a file WITHOUT filling in the title
    const file = new File(['content'], 'manuscript.docx', {
      type: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    })
    const fileInput = screen.getByLabelText(/upload document file/i)
    await user.upload(fileInput, file)

    // Handler should still be called (title arg may be undefined or omitted)
    expect(defaultProps.onFileUpload).toHaveBeenCalled()
  })
})
