import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { axe } from 'jest-axe'
import { SuggestionCard } from '@/components/SuggestionCard'
import type { Paragraph, Suggestion } from '@/types'

const SAMPLE_PARAGRAPHS: Paragraph[] = [
  { id: 'p-1', text: 'The quick brown fox jumps over the lazy dog.' },
  { id: 'p-2', text: 'This is a simple test document with enough text to cover the range.' },
  { id: 'p-3', text: 'Additional sentences provide context for suggestion extraction.' },
]

const createSuggestion = (overrides?: Partial<Suggestion>): Suggestion => ({
  id: 'sug-1',
  documentId: 'doc-1',
  paragraphId: 'p-1',
  rationale: 'This passage reads as overly compressed factual summary without narrative color.',
  proposedChange: 'Consider expanding with sensory detail and scene-setting to draw the reader in.',
  status: 'Pending',
  ...overrides,
})

describe('SuggestionCard', () => {
  const defaultProps = {
    suggestion: createSuggestion(),
    number: 1,
    isActive: false,
    isHovered: false,
    onStatusChange: vi.fn(),
    onClick: vi.fn(),
    onHoverChange: vi.fn(),
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('rendering', () => {
    it('renders as an article with accessible label', () => {
      render(<SuggestionCard {...defaultProps} />)

      expect(screen.getByRole('article')).toBeInTheDocument()
    })

    it('displays the suggestion rationale', () => {
      render(<SuggestionCard {...defaultProps} />)

      expect(screen.getByText(/overly compressed factual summary/i)).toBeInTheDocument()
    })

    it('shows the status badge', () => {
      render(<SuggestionCard {...defaultProps} />)

      expect(screen.getByText('Pending')).toBeInTheDocument()
    })

    it('shows accepted badge for accepted suggestions', () => {
      render(
        <SuggestionCard
          {...defaultProps}
          suggestion={createSuggestion({ status: 'Accepted' })}
        />
      )

      expect(screen.getByText('Accepted')).toBeInTheDocument()
    })

    it('shows rejected badge for rejected suggestions', () => {
      render(
        <SuggestionCard
          {...defaultProps}
          suggestion={createSuggestion({ status: 'Rejected' })}
        />
      )

      expect(screen.getByText('Rejected')).toBeInTheDocument()
    })
  })

  describe('expand/collapse', () => {
    it.each([true, false])('respects reduced motion (%s) when scrolling an active suggestion', (reducedMotion) => {
      const scrollIntoView = vi.fn()
      const originalScroll = HTMLElement.prototype.scrollIntoView
      HTMLElement.prototype.scrollIntoView = scrollIntoView
      const media = vi.spyOn(window, 'matchMedia').mockImplementation((query) => ({
        matches: reducedMotion, media: query, onchange: null,
        addListener: vi.fn(), removeListener: vi.fn(),
        addEventListener: vi.fn(), removeEventListener: vi.fn(), dispatchEvent: vi.fn(),
      }))
      try {
        render(<SuggestionCard {...defaultProps} isActive />)
        expect(scrollIntoView).toHaveBeenCalledWith({
          behavior: reducedMotion ? 'instant' : 'smooth', block: 'nearest',
        })
      } finally {
        media.mockRestore()
        HTMLElement.prototype.scrollIntoView = originalScroll
      }
    })

    it('shows expand button', () => {
      render(<SuggestionCard {...defaultProps} />)

      expect(
        screen.getByRole('button', { name: /expand suggestion/i })
      ).toBeInTheDocument()
    })

    it('shows proposed change when expanded', async () => {
      const user = userEvent.setup()
      render(<SuggestionCard {...defaultProps} />)

      await user.click(screen.getByRole('button', { name: /expand suggestion/i }))

      expect(screen.getByText(/sensory detail and scene-setting/i)).toBeInTheDocument()
    })

    it('toggles to collapse button when expanded', async () => {
      const user = userEvent.setup()
      render(<SuggestionCard {...defaultProps} />)

      await user.click(screen.getByRole('button', { name: /expand suggestion/i }))

      expect(
        screen.getByRole('button', { name: /collapse suggestion/i })
      ).toBeInTheDocument()
    })
  })

  describe('status actions', () => {
    it('shows Accept, Reject, Modify buttons when expanded and pending', async () => {
      const user = userEvent.setup()
      render(<SuggestionCard {...defaultProps} />)

      await user.click(screen.getByRole('button', { name: /expand suggestion/i }))

      expect(screen.getByRole('button', { name: /accept/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /reject/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /modify/i })).toBeInTheDocument()
    })

    it('calls onStatusChange with Accepted when Accept is clicked', async () => {
      const user = userEvent.setup()
      render(<SuggestionCard {...defaultProps} />)

      await user.click(screen.getByRole('button', { name: /expand suggestion/i }))
      await user.click(screen.getByRole('button', { name: /accept/i }))

      expect(defaultProps.onStatusChange).toHaveBeenCalledWith('sug-1', 'Accepted')
    })

    it('calls onStatusChange with Rejected when Reject is clicked', async () => {
      const user = userEvent.setup()
      render(<SuggestionCard {...defaultProps} />)

      await user.click(screen.getByRole('button', { name: /expand suggestion/i }))
      await user.click(screen.getByRole('button', { name: /reject/i }))

      expect(defaultProps.onStatusChange).toHaveBeenCalledWith('sug-1', 'Rejected')
    })

    it('opens edit mode when Modify is clicked', async () => {
      const user = userEvent.setup()
      render(<SuggestionCard {...defaultProps} />)

      await user.click(screen.getByRole('button', { name: /expand suggestion/i }))
      await user.click(screen.getByRole('button', { name: /modify/i }))

      expect(
        screen.getByLabelText(/modified suggestion text/i)
      ).toBeInTheDocument()
    })

    it('calls onStatusChange with Modified and text when Save is clicked in edit mode', async () => {
      const user = userEvent.setup()
      render(<SuggestionCard {...defaultProps} />)

      await user.click(screen.getByRole('button', { name: /expand suggestion/i }))
      await user.click(screen.getByRole('button', { name: /modify/i }))

      const textarea = screen.getByLabelText(/modified suggestion text/i)
      await user.clear(textarea)
      await user.type(textarea, 'My custom revision with more narrative air.')

      await user.click(screen.getByRole('button', { name: /save/i }))

      expect(defaultProps.onStatusChange).toHaveBeenCalledWith(
        'sug-1',
        'Modified',
        'My custom revision with more narrative air.'
      )
    })

    it('does not show action buttons for accepted suggestions', async () => {
      const user = userEvent.setup()
      render(
        <SuggestionCard
          {...defaultProps}
          suggestion={createSuggestion({ status: 'Accepted' })}
        />
      )

      await user.click(screen.getByRole('button', { name: /expand suggestion/i }))

      expect(screen.queryByRole('button', { name: /^accept$/i })).not.toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /^reject$/i })).not.toBeInTheDocument()
    })

    it('cancels edit mode without saving', async () => {
      const user = userEvent.setup()
      render(<SuggestionCard {...defaultProps} />)

      await user.click(screen.getByRole('button', { name: /expand suggestion/i }))
      await user.click(screen.getByRole('button', { name: /modify/i }))

      expect(screen.getByLabelText(/modified suggestion text/i)).toBeInTheDocument()

      await user.click(screen.getByRole('button', { name: /cancel/i }))

      expect(screen.queryByLabelText(/modified suggestion text/i)).not.toBeInTheDocument()
      expect(defaultProps.onStatusChange).not.toHaveBeenCalled()
    })
  })

  describe('click handling', () => {
    it('calls onClick when card header is clicked', async () => {
      const user = userEvent.setup()
      render(<SuggestionCard {...defaultProps} />)

      await user.click(screen.getByRole('button', { name: /overly compressed/i }))
      expect(defaultProps.onClick).toHaveBeenCalledWith('sug-1')
    })
  })

  describe('keyboard accessibility', () => {
    it('selects the suggestion with the keyboard independently of its disclosure', async () => {
      const user = userEvent.setup()
      render(<SuggestionCard {...defaultProps} />)
      screen.getByRole('button', { name: /overly compressed/i }).focus()
      await user.keyboard('{Enter}')
      expect(defaultProps.onClick).toHaveBeenCalledWith('sug-1')
      const disclosure = screen.getByRole('button', { name: /expand suggestion/i })
      expect(disclosure).toHaveAttribute('aria-expanded', 'false')
      await user.click(disclosure)
      expect(disclosure).toHaveAttribute('aria-expanded', 'true')
      expect(document.getElementById(disclosure.getAttribute('aria-controls') ?? '')).toBeInTheDocument()
    })
  })

  describe('accessibility', () => {
    it('passes axe accessibility checks', async () => {
      const { container } = render(<SuggestionCard {...defaultProps} />)

      const results = await axe(container)
      expect(results).toHaveNoViolations()
    })
  })

  describe('original text display', () => {
    it('shows original text when paragraphs are provided and card is expanded', async () => {
      const user = userEvent.setup()
      render(
        <SuggestionCard
          {...defaultProps}
          paragraphs={SAMPLE_PARAGRAPHS}
        />
      )

      await user.click(screen.getByRole('button', { name: /expand suggestion/i }))

      expect(screen.getByText('Original text:')).toBeInTheDocument()
      expect(
        screen.getByText('The quick brown fox jumps over the lazy dog.')
      ).toBeInTheDocument()
    })

    it('does not show original text when paragraphs are not provided', async () => {
      const user = userEvent.setup()
      render(<SuggestionCard {...defaultProps} />)

      await user.click(screen.getByRole('button', { name: /expand suggestion/i }))

      expect(screen.queryByText('Original text:')).not.toBeInTheDocument()
    })
  })

  describe('reanalyze button', () => {
    it('shows Analyze button when onReanalyze is provided and card is expanded (pending)', async () => {
      const user = userEvent.setup()
      const onReanalyze = vi.fn()
      render(
        <SuggestionCard {...defaultProps} onReanalyze={onReanalyze} />
      )

      await user.click(screen.getByRole('button', { name: /expand suggestion/i }))

      expect(screen.getByRole('button', { name: /analyze/i })).toBeInTheDocument()
    })

    it('does not show Analyze button when onReanalyze is not provided', async () => {
      const user = userEvent.setup()
      render(<SuggestionCard {...defaultProps} />)

      await user.click(screen.getByRole('button', { name: /expand suggestion/i }))

      expect(screen.queryByRole('button', { name: /^analyze$/i })).not.toBeInTheDocument()
    })

    it('calls onReanalyze with paragraphId when Analyze is clicked (pending)', async () => {
      const user = userEvent.setup()
      const onReanalyze = vi.fn()
      render(
        <SuggestionCard {...defaultProps} onReanalyze={onReanalyze} />
      )

      await user.click(screen.getByRole('button', { name: /expand suggestion/i }))
      await user.click(screen.getByRole('button', { name: /analyze/i }))

      expect(onReanalyze).toHaveBeenCalledWith('p-1')
    })

    it('shows Analyze button for accepted suggestions when onReanalyze is provided', async () => {
      const user = userEvent.setup()
      const onReanalyze = vi.fn()
      render(
        <SuggestionCard
          {...defaultProps}
          suggestion={createSuggestion({ status: 'Accepted' })}
          onReanalyze={onReanalyze}
        />
      )

      await user.click(screen.getByRole('button', { name: /expand suggestion/i }))

      expect(screen.getByRole('button', { name: /analyze/i })).toBeInTheDocument()
    })

    it('calls onReanalyze with paragraphId when Analyze is clicked (accepted)', async () => {
      const user = userEvent.setup()
      const onReanalyze = vi.fn()
      render(
        <SuggestionCard
          {...defaultProps}
          suggestion={createSuggestion({ status: 'Accepted' })}
          onReanalyze={onReanalyze}
        />
      )

      await user.click(screen.getByRole('button', { name: /expand suggestion/i }))
      await user.click(screen.getByRole('button', { name: /analyze/i }))

      expect(onReanalyze).toHaveBeenCalledWith('p-1')
    })
  })
})
