import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { axe } from 'jest-axe'
import { ReplaceAnalysisConfirmationDialog } from '@/components/ReplaceAnalysisConfirmationDialog'
import { buildSummaryMessage } from '@/lib/replaceAnalysisSummary'
import type { Suggestion } from '@/types'

const createSuggestion = (overrides?: Partial<Suggestion>): Suggestion => ({
  id: 'sug-1',
  userId: 'user-1',
  documentId: 'doc-1',
  paragraphId: 'p-1',
  rationale: 'rationale',
  proposedChange: 'proposed change text',
  status: 'Accepted',
  ...overrides,
})

describe('buildSummaryMessage', () => {
  it('shows accepted merged + pending discarded', () => {
    const msg = buildSummaryMessage(2, 3, 0)
    expect(msg).toBe(
      'Your 2 accepted suggestions will be merged into the manuscript. The 3 pending suggestions will be discarded.'
    )
  })

  it('shows accepted merged + rejected discarded', () => {
    const msg = buildSummaryMessage(1, 0, 3)
    expect(msg).toBe(
      'Your 1 accepted suggestion will be merged into the manuscript. The 3 rejected suggestions will be discarded.'
    )
  })

  it('shows accepted merged + pending and rejected discarded', () => {
    const msg = buildSummaryMessage(1, 2, 3)
    expect(msg).toBe(
      'Your 1 accepted suggestion will be merged into the manuscript. The 2 pending and 3 rejected suggestions will be discarded.'
    )
  })

  it('shows accepted only when no pending or rejected', () => {
    const msg = buildSummaryMessage(2, 0, 0)
    expect(msg).toBe(
      'Your 2 accepted suggestions will be merged into the manuscript before re-analysis.'
    )
  })

  it('shows singular accepted with no discards', () => {
    const msg = buildSummaryMessage(1, 0, 0)
    expect(msg).toBe(
      'Your 1 accepted suggestion will be merged into the manuscript before re-analysis.'
    )
  })

  it('shows only pending discarded when no accepted', () => {
    const msg = buildSummaryMessage(0, 3, 0)
    expect(msg).toBe(
      'All 3 pending suggestions will be discarded and replaced with new analysis results.'
    )
  })

  it('shows only rejected discarded when no accepted', () => {
    const msg = buildSummaryMessage(0, 0, 3)
    expect(msg).toBe(
      'All 3 rejected suggestions will be discarded and replaced with new analysis results.'
    )
  })

  it('shows pending and rejected discarded when no accepted', () => {
    const msg = buildSummaryMessage(0, 1, 2)
    expect(msg).toBe(
      'All 1 pending and 2 rejected suggestions will be discarded and replaced with new analysis results.'
    )
  })

  it('shows singular pending discarded', () => {
    const msg = buildSummaryMessage(0, 1, 0)
    expect(msg).toBe(
      'All 1 pending suggestion will be discarded and replaced with new analysis results.'
    )
  })

  it('shows singular rejected discarded', () => {
    const msg = buildSummaryMessage(0, 0, 1)
    expect(msg).toBe(
      'All 1 rejected suggestion will be discarded and replaced with new analysis results.'
    )
  })

  it('shows fallback when all counts are zero', () => {
    const msg = buildSummaryMessage(0, 0, 0)
    expect(msg).toBe(
      'All existing suggestions will be replaced with new analysis results.'
    )
  })
})

describe('ReplaceAnalysisConfirmationDialog', () => {
  const defaultProps = {
    open: true,
    onOpenChange: vi.fn(),
    acceptedSuggestions: [] as Suggestion[],
    pendingCount: 0,
    rejectedCount: 3,
    onConfirm: vi.fn(),
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('rendering', () => {
    it('renders dialog title when open', () => {
      render(<ReplaceAnalysisConfirmationDialog {...defaultProps} />)

      expect(screen.getByText('Replace Analysis?')).toBeInTheDocument()
    })

    it('does not render when closed', () => {
      render(<ReplaceAnalysisConfirmationDialog {...defaultProps} open={false} />)

      expect(screen.queryByText('Replace Analysis?')).not.toBeInTheDocument()
    })

    it('renders summary for rejected-only case', () => {
      render(<ReplaceAnalysisConfirmationDialog {...defaultProps} rejectedCount={3} />)

      expect(
        screen.getByText(/3 rejected suggestions will be discarded/)
      ).toBeInTheDocument()
    })

    it('renders summary with accepted and pending', () => {
      render(
        <ReplaceAnalysisConfirmationDialog
          {...defaultProps}
          acceptedSuggestions={[createSuggestion()]}
          pendingCount={2}
          rejectedCount={0}
        />
      )

      expect(screen.getByText(/1 accepted suggestion will be merged/)).toBeInTheDocument()
      expect(screen.getByText(/2 pending suggestion/)).toBeInTheDocument()
    })

    it('shows accepted suggestions list when present', () => {
      const accepted = [
        createSuggestion({ id: 's1', proposedChange: 'First change' }),
        createSuggestion({ id: 's2', proposedChange: 'Second change' }),
      ]

      render(
        <ReplaceAnalysisConfirmationDialog
          {...defaultProps}
          acceptedSuggestions={accepted}
          pendingCount={1}
        />
      )

      expect(screen.getByText('Accepted Suggestions to Merge (2)')).toBeInTheDocument()
      expect(screen.getByText('First change')).toBeInTheDocument()
      expect(screen.getByText('Second change')).toBeInTheDocument()
    })

    it('hides accepted suggestions list when none exist', () => {
      render(
        <ReplaceAnalysisConfirmationDialog
          {...defaultProps}
          acceptedSuggestions={[]}
          rejectedCount={2}
        />
      )

      expect(screen.queryByText(/Accepted Suggestions to Merge/)).not.toBeInTheDocument()
    })

    it('renders cancel and confirm buttons', () => {
      render(<ReplaceAnalysisConfirmationDialog {...defaultProps} />)

      expect(screen.getByRole('button', { name: /cancel/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /replace & analyze/i })).toBeInTheDocument()
    })
  })

  describe('interactions', () => {
    it('calls onConfirm and closes when Replace & Analyze is clicked', async () => {
      const user = userEvent.setup()
      const onConfirm = vi.fn()
      const onOpenChange = vi.fn()

      render(
        <ReplaceAnalysisConfirmationDialog
          {...defaultProps}
          onConfirm={onConfirm}
          onOpenChange={onOpenChange}
        />
      )

      await user.click(screen.getByRole('button', { name: /replace & analyze/i }))

      expect(onConfirm).toHaveBeenCalledOnce()
      expect(onOpenChange).toHaveBeenCalledWith(false)
    })

    it('calls onOpenChange with false when Cancel is clicked', async () => {
      const user = userEvent.setup()
      const onOpenChange = vi.fn()

      render(
        <ReplaceAnalysisConfirmationDialog
          {...defaultProps}
          onOpenChange={onOpenChange}
        />
      )

      await user.click(screen.getByRole('button', { name: /cancel/i }))

      expect(onOpenChange).toHaveBeenCalledWith(false)
    })

    it('collapses accepted list when toggle button is clicked', async () => {
      const user = userEvent.setup()
      const accepted = [createSuggestion({ proposedChange: 'Visible change' })]

      render(
        <ReplaceAnalysisConfirmationDialog
          {...defaultProps}
          acceptedSuggestions={accepted}
          pendingCount={1}
        />
      )

      expect(screen.getByText('Visible change')).toBeInTheDocument()

      await user.click(screen.getByText(/Accepted Suggestions to Merge/))

      expect(screen.queryByText('Visible change')).not.toBeInTheDocument()
    })
  })

  describe('accessibility', () => {
    it('has no axe violations', async () => {
      const { container } = render(
        <ReplaceAnalysisConfirmationDialog
          {...defaultProps}
          acceptedSuggestions={[createSuggestion()]}
          pendingCount={2}
          rejectedCount={1}
        />
      )

      const results = await axe(container)
      expect(results).toHaveNoViolations()
    })
  })
})
