import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { AnalysisDialog } from '@/components/AnalysisDialog'

describe('AnalysisDialog', () => {
  const defaultProps = {
    open: true,
    onOpenChange: vi.fn(),
    isAnalyzing: false,
    progress: '',
    onAnalyze: vi.fn(),
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders tone before guidance', () => {
    render(<AnalysisDialog {...defaultProps} />)

    const toneLabel = screen.getByText('Tone')
    const guidanceLabel = screen.getByText('Guidance (optional)')

    expect(
      toneLabel.compareDocumentPosition(guidanceLabel) &
        Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy()
  })

  it('shows "Analyze Manuscript" title by default', () => {
    render(<AnalysisDialog {...defaultProps} />)

    expect(screen.getByText('Analyze Manuscript')).toBeInTheDocument()
  })

  it('shows "Analyze Paragraph" title when paragraphMode is true', () => {
    render(<AnalysisDialog {...defaultProps} paragraphMode />)

    expect(screen.getByText('Analyze Paragraph')).toBeInTheDocument()
  })

  it('shows paragraph-specific description when paragraphMode is true', () => {
    render(<AnalysisDialog {...defaultProps} paragraphMode />)

    expect(
      screen.getByText(/re-analyze this paragraph/i)
    ).toBeInTheDocument()
  })
})