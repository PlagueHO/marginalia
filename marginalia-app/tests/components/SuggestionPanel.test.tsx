import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { axe } from 'jest-axe'
import { SuggestionPanel } from '@/components/SuggestionPanel'
import type { Paragraph, Suggestion, SuggestionStatus } from '@/types'

function createSuggestion(
  id: string,
  status: SuggestionStatus = 'Pending',
  paragraphId = 'p-1'
): Suggestion {
  return {
    id,
    documentId: 'doc-1',
    paragraphId,
    rationale: `Rationale for suggestion ${id}`,
    proposedChange: `Proposed change for ${id}`,
    status,
  }
}

const sampleParagraphs: Paragraph[] = [
  { id: 'p-1', text: 'Sample document content for testing suggestion display.' },
]

describe('SuggestionPanel', () => {
  const suggestions = [
    createSuggestion('sug-1', 'Pending'),
    createSuggestion('sug-2', 'Pending'),
    createSuggestion('sug-3', 'Accepted'),
    createSuggestion('sug-4', 'Rejected'),
  ]

  const counts = {
    Pending: 2,
    Accepted: 1,
    Rejected: 1,
    Modified: 0,
    total: 4,
  }

  const defaultProps = {
    suggestions,
    filteredSuggestions: suggestions,
    filter: 'All' as SuggestionStatus | 'All',
    activeSuggestionId: null,
    hoveredSuggestionId: null,
    suggestionNumbers: new Map([
      ['sug-1', 1],
      ['sug-2', 2],
      ['sug-3', 3],
      ['sug-4', 4],
    ]),
    paragraphs: sampleParagraphs,
    counts,
    onFilterChange: vi.fn(),
    onStatusChange: vi.fn(),
    onSuggestionClick: vi.fn(),
    onSuggestionHover: vi.fn(),
    onAcceptAll: vi.fn().mockResolvedValue(undefined),
    onRejectAll: vi.fn().mockResolvedValue(undefined),
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('rendering', () => {
    it('renders the suggestions panel with accessible label', () => {
      render(<SuggestionPanel {...defaultProps} />)

      expect(
        screen.getByRole('complementary', { name: /suggestions panel/i })
      ).toBeInTheDocument()
    })

    it('displays "Suggestions" heading', () => {
      render(<SuggestionPanel {...defaultProps} />)

      expect(screen.getByText('Suggestions')).toBeInTheDocument()
    })

    it('renders suggestion cards for all filtered suggestions', () => {
      render(<SuggestionPanel {...defaultProps} />)

      expect(screen.getByText(/rationale for suggestion sug-1/i)).toBeInTheDocument()
      expect(screen.getByText(/rationale for suggestion sug-2/i)).toBeInTheDocument()
      expect(screen.getByText(/rationale for suggestion sug-3/i)).toBeInTheDocument()
      expect(screen.getByText(/rationale for suggestion sug-4/i)).toBeInTheDocument()
    })

    it('renders paragraph connector rails only for paragraphs with multiple suggestions', () => {
      const mixedSuggestions = [
        createSuggestion('sug-1', 'Pending', 'p-1'),
        createSuggestion('sug-2', 'Pending', 'p-1'),
        createSuggestion('sug-3', 'Accepted', 'p-2'),
      ]

      const { container } = render(
        <SuggestionPanel
          {...defaultProps}
          suggestions={mixedSuggestions}
          filteredSuggestions={mixedSuggestions}
          counts={{
            Pending: 2,
            Accepted: 1,
            Rejected: 0,
            Modified: 0,
            total: 3,
          }}
        />
      )

      expect(container.querySelectorAll('[data-testid="paragraph-suggestion-connector"]').length).toBe(1)
    })
  })

  describe('filtering', () => {
    it('renders filter tabs', () => {
      render(<SuggestionPanel {...defaultProps} />)

      expect(screen.getByRole('tab', { name: /all/i })).toBeInTheDocument()
      expect(screen.getByRole('tab', { name: /pending/i })).toBeInTheDocument()
      expect(screen.getByRole('tab', { name: /accepted/i })).toBeInTheDocument()
      expect(screen.getByRole('tab', { name: /rejected/i })).toBeInTheDocument()
      expect(screen.getByRole('tab', { name: /modified/i })).toBeInTheDocument()
    })

    it('shows total count in All tab', () => {
      render(<SuggestionPanel {...defaultProps} />)

      expect(screen.getByText(/all \(4\)/i)).toBeInTheDocument()
    })

    it('keeps filters in one scrollable row without compressing their labels', () => {
      render(<SuggestionPanel {...defaultProps} />)

      expect(
        screen.getByRole('tablist', { name: /filter suggestions by status/i })
      ).toHaveClass('flex', 'flex-nowrap', 'overflow-x-auto', 'overflow-y-hidden')
      for (const tab of screen.getAllByRole('tab')) {
        expect(tab).toHaveClass(
          'min-w-max',
          'shrink-0',
          'min-h-10',
          'pointer-coarse:min-h-11',
          'pointer-coarse:min-w-11',
        )
      }
    })

    it('supports arrow, Home, and End keys across the single-row filters', async () => {
      const user = userEvent.setup()
      render(<SuggestionPanel {...defaultProps} />)

      const allTab = screen.getByRole('tab', { name: /all/i })
      await user.click(allTab)
      await user.keyboard('{ArrowRight}')

      expect(screen.getByRole('tab', { name: /pending/i })).toHaveFocus()
      expect(defaultProps.onFilterChange).toHaveBeenLastCalledWith('Pending')

      await user.keyboard('{End}')

      expect(screen.getByRole('tab', { name: /modified/i })).toHaveFocus()
      expect(defaultProps.onFilterChange).toHaveBeenLastCalledWith('Modified')

      await user.keyboard('{Home}')

      expect(allTab).toHaveFocus()
    })

    it('calls onFilterChange when a tab is clicked', async () => {
      const user = userEvent.setup()
      render(<SuggestionPanel {...defaultProps} />)

      await user.click(screen.getByRole('tab', { name: /pending/i }))

      expect(defaultProps.onFilterChange).toHaveBeenCalledWith('Pending')
    })
  })

  describe('empty state', () => {
    it('shows empty message when no suggestions match filter', () => {
      render(
        <SuggestionPanel
          {...defaultProps}
          filteredSuggestions={[]}
        />
      )

      expect(screen.getByText(/no.*suggestions/i)).toBeInTheDocument()
    })
  })

  describe('unanalyzed state', () => {
    const emptyCounts = { Pending: 0, Accepted: 0, Rejected: 0, Modified: 0, total: 0 }

    it('shows analyze instruction when isUnanalyzed is true', () => {
      render(
        <SuggestionPanel
          {...defaultProps}
          suggestions={[]}
          filteredSuggestions={[]}
          counts={emptyCounts}
          isUnanalyzed
          onAnalyze={vi.fn()}
        />
      )

      expect(
        screen.getByText(/analyze the manuscript to generate suggestions/i)
      ).toBeInTheDocument()
    })

    it('shows Analyze button when isUnanalyzed and onAnalyze is provided', () => {
      render(
        <SuggestionPanel
          {...defaultProps}
          suggestions={[]}
          filteredSuggestions={[]}
          counts={emptyCounts}
          isUnanalyzed
          onAnalyze={vi.fn()}
        />
      )

      expect(screen.getByRole('button', { name: /analyze/i })).toBeInTheDocument()
    })

    it('calls onAnalyze when Analyze button is clicked', async () => {
      const user = userEvent.setup()
      const onAnalyze = vi.fn()
      render(
        <SuggestionPanel
          {...defaultProps}
          suggestions={[]}
          filteredSuggestions={[]}
          counts={emptyCounts}
          isUnanalyzed
          onAnalyze={onAnalyze}
        />
      )

      await user.click(screen.getByRole('button', { name: /analyze/i }))

      expect(onAnalyze).toHaveBeenCalledOnce()
    })

    it('does not show filter tabs when isUnanalyzed', () => {
      render(
        <SuggestionPanel
          {...defaultProps}
          suggestions={[]}
          filteredSuggestions={[]}
          counts={emptyCounts}
          isUnanalyzed
          onAnalyze={vi.fn()}
        />
      )

      expect(screen.queryByRole('tab', { name: /all/i })).not.toBeInTheDocument()
    })

    it('does not show batch actions when isUnanalyzed', () => {
      render(
        <SuggestionPanel
          {...defaultProps}
          suggestions={[]}
          filteredSuggestions={[]}
          counts={emptyCounts}
          isUnanalyzed
          onAnalyze={vi.fn()}
        />
      )

      expect(screen.queryByRole('button', { name: /accept all/i })).not.toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /reject all/i })).not.toBeInTheDocument()
    })
  })

  describe('batch actions', () => {
    it('shares batch-action width only when the actions are in a horizontal row', () => {
      render(<SuggestionPanel {...defaultProps} />)

      for (const name of [/accept all/i, /reject all/i]) {
        const button = screen.getByRole('button', { name })
        expect(button).toHaveClass('sm:flex-1')
        expect(button).not.toHaveClass('flex-1')
      }
    })

    it('renders Accept All button with pending count', () => {
      render(<SuggestionPanel {...defaultProps} />)

      expect(
        screen.getByRole('button', { name: /accept all \(2\)/i })
      ).toBeInTheDocument()
    })

    it('renders Reject All button with pending count', () => {
      render(<SuggestionPanel {...defaultProps} />)

      expect(
        screen.getByRole('button', { name: /reject all \(2\)/i })
      ).toBeInTheDocument()
    })

    it('calls onAcceptAll when Accept All is clicked', async () => {
      const user = userEvent.setup()
      render(<SuggestionPanel {...defaultProps} />)

      await user.click(screen.getByRole('button', { name: /accept all/i }))

      expect(defaultProps.onAcceptAll).toHaveBeenCalledOnce()
    })

    it('calls onRejectAll when Reject All is clicked', async () => {
      const user = userEvent.setup()
      render(<SuggestionPanel {...defaultProps} />)

      await user.click(screen.getByRole('button', { name: /reject all/i }))

      expect(defaultProps.onRejectAll).toHaveBeenCalledOnce()
    })

    it('does not show batch buttons when no pending suggestions', () => {
      const noPendingCounts = { Pending: 0, Accepted: 3, Rejected: 1, Modified: 0, total: 4 }
      render(
        <SuggestionPanel
          {...defaultProps}
          counts={noPendingCounts}
        />
      )

      expect(screen.queryByRole('button', { name: /accept all/i })).not.toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /reject all/i })).not.toBeInTheDocument()
    })
  })

  describe('counts display', () => {
    it('displays pending count badge', () => {
      render(<SuggestionPanel {...defaultProps} />)

      expect(screen.getByText(/2 pending/i)).toBeInTheDocument()
    })

    it('displays accepted count badge', () => {
      render(<SuggestionPanel {...defaultProps} />)

      expect(screen.getByText(/1 accepted/i)).toBeInTheDocument()
    })

    it('displays rejected count badge', () => {
      render(<SuggestionPanel {...defaultProps} />)

      expect(screen.getByText(/1 rejected/i)).toBeInTheDocument()
    })
  })

  describe('accessibility', () => {
    it('passes axe accessibility checks', async () => {
      const { container } = render(<SuggestionPanel {...defaultProps} />)

      const results = await axe(container)
      expect(results).toHaveNoViolations()
    })
  })
})
