import { renderHook, act } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { useSuggestions } from '@/hooks/useSuggestions'
import type { Paragraph, Suggestion } from '@/types'

describe('useSuggestions', () => {
  it('keeps suggestions ordered by paragraph order', () => {
    const paragraphs: Paragraph[] = [
      { id: 'p-2', text: 'Second paragraph' },
      { id: 'p-1', text: 'First paragraph' },
      { id: 'p-3', text: 'Third paragraph' },
    ]

    const suggestions: Suggestion[] = [
      {
        id: 's-3',
        userId: 'user-1',
        documentId: 'doc-1',
        paragraphId: 'p-3',
        rationale: 'Third',
        proposedChange: 'Third change',
        status: 'Pending',
      },
      {
        id: 's-1',
        userId: 'user-1',
        documentId: 'doc-1',
        paragraphId: 'p-1',
        rationale: 'First',
        proposedChange: 'First change',
        status: 'Pending',
      },
      {
        id: 's-2',
        userId: 'user-1',
        documentId: 'doc-1',
        paragraphId: 'p-2',
        rationale: 'Second',
        proposedChange: 'Second change',
        status: 'Pending',
      },
    ]

    const { result } = renderHook(() => useSuggestions())

    act(() => {
      result.current.setParagraphs(paragraphs)
      result.current.setSuggestions(suggestions)
    })

    expect(result.current.suggestions.map((s) => s.id)).toEqual([
      's-2',
      's-1',
      's-3',
    ])
    expect(result.current.filteredSuggestions.map((s) => s.id)).toEqual([
      's-2',
      's-1',
      's-3',
    ])
  })

  it('keeps filtered suggestions ordered by paragraph order', () => {
    const paragraphs: Paragraph[] = [
      { id: 'p-1', text: 'First paragraph' },
      { id: 'p-2', text: 'Second paragraph' },
      { id: 'p-3', text: 'Third paragraph' },
    ]

    const suggestions: Suggestion[] = [
      {
        id: 's-3',
        userId: 'user-1',
        documentId: 'doc-1',
        paragraphId: 'p-3',
        rationale: 'Third',
        proposedChange: 'Third change',
        status: 'Accepted',
      },
      {
        id: 's-2',
        userId: 'user-1',
        documentId: 'doc-1',
        paragraphId: 'p-2',
        rationale: 'Second',
        proposedChange: 'Second change',
        status: 'Accepted',
      },
      {
        id: 's-1',
        userId: 'user-1',
        documentId: 'doc-1',
        paragraphId: 'p-1',
        rationale: 'First',
        proposedChange: 'First change',
        status: 'Pending',
      },
    ]

    const { result } = renderHook(() => useSuggestions())

    act(() => {
      result.current.setParagraphs(paragraphs)
      result.current.setSuggestions(suggestions)
      result.current.setFilter('Accepted')
    })

    expect(result.current.filteredSuggestions.map((s) => s.id)).toEqual([
      's-2',
      's-3',
    ])
  })
})
