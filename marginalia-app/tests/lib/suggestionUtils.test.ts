import { describe, expect, it } from 'vitest'
import { applyAcceptedSuggestions, getAcceptedSuggestionsCharacterCount } from '@/lib/suggestionUtils'
import type { Paragraph, Suggestion } from '@/types'

const createSuggestion = (overrides?: Partial<Suggestion>): Suggestion => ({
  id: 'sug-1',
  userId: 'user-1',
  documentId: 'doc-1',
  paragraphId: 'p-1',
  rationale: 'rationale',
  proposedChange: '',
  status: 'Pending',
  ...overrides,
})

const makeParagraphs = (...texts: string[]): Paragraph[] =>
  texts.map((text, i) => ({ id: `p-${i + 1}`, text }))

describe('suggestionUtils', () => {
  it('returns original content when no suggestions are accepted', () => {
    const paragraphs = makeParagraphs('The quick brown fox')
    const suggestions = [
      createSuggestion({
        id: 'sug-pending',
        paragraphId: 'p-1',
        proposedChange: 'slow',
        status: 'Pending',
      }),
    ]

    expect(applyAcceptedSuggestions(paragraphs, suggestions)).toBe('The quick brown fox')
    expect(getAcceptedSuggestionsCharacterCount(paragraphs, suggestions)).toBe('The quick brown fox'.length)
  })

  it('applies a single accepted suggestion', () => {
    const paragraphs = makeParagraphs('Hello world')
    const suggestions = [
      createSuggestion({
        id: 'sug-accepted',
        paragraphId: 'p-1',
        proposedChange: 'Hello everyone',
        status: 'Accepted',
      }),
    ]

    expect(applyAcceptedSuggestions(paragraphs, suggestions)).toBe('Hello everyone')
    expect(getAcceptedSuggestionsCharacterCount(paragraphs, suggestions)).toBe(14)
  })

  it('applies multiple accepted suggestions without index shift issues', () => {
    const paragraphs = makeParagraphs('abc', 'def', 'ghi')
    const suggestions = [
      createSuggestion({
        id: 'sug-left',
        paragraphId: 'p-1',
        proposedChange: 'ABCD',
        status: 'Accepted',
      }),
      createSuggestion({
        id: 'sug-right',
        paragraphId: 'p-3',
        proposedChange: 'G',
        status: 'Accepted',
      }),
    ]

    expect(applyAcceptedSuggestions(paragraphs, suggestions)).toBe('ABCD\n\ndef\n\nG')
  })

  it('returns empty string for empty paragraphs array', () => {
    const paragraphs: Paragraph[] = []
    const suggestions = [
      createSuggestion({
        id: 'sug-orphan',
        paragraphId: 'p-99',
        proposedChange: 'replacement',
        status: 'Accepted',
      }),
    ]

    expect(applyAcceptedSuggestions(paragraphs, suggestions)).toBe('')
    expect(getAcceptedSuggestionsCharacterCount(paragraphs, suggestions)).toBe(0)
  })
})
