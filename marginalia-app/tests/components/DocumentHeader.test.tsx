import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { DocumentHeader } from '@/components/DocumentHeader'
import type { Document, Suggestion } from '@/types'

const createDocument = (overrides?: Partial<Document>): Document => ({
  id: 'doc-1',
  userId: 'user-1',
  filename: 'manuscript.docx',
  source: 'Local',
  paragraphs: [{ id: 'p-1', text: 'Hello world' }],
  title: 'My Manuscript',
  status: 'Draft',
  createdAt: '2026-01-01T00:00:00.000Z',
  updatedAt: '2026-01-01T00:00:00.000Z',
  suggestions: [],
  ...overrides,
})

const createSuggestion = (overrides?: Partial<Suggestion>): Suggestion => ({
  id: 'sug-1',
  userId: 'user-1',
  documentId: 'doc-1',
  paragraphId: 'p-1',
  rationale: 'Improve wording',
  proposedChange: 'Hello everyone',
  status: 'Pending',
  ...overrides,
})

describe('DocumentHeader', () => {
  it('shows original and with-suggestions counts', () => {
    const document = createDocument()
    const suggestions = [createSuggestion({ status: 'Accepted' })]

    render(<DocumentHeader document={document} suggestions={suggestions} />)

    expect(screen.getByText(/Original/)).toBeInTheDocument()
    expect(screen.getByText('11')).toBeInTheDocument()
    expect(screen.getByText(/With accepted/)).toBeInTheDocument()
    expect(screen.getByText('14')).toBeInTheDocument()
  })

  it('does not change with-suggestions count for pending suggestions', () => {
    const document = createDocument()
    const suggestions = [createSuggestion({ status: 'Pending' })]

    render(<DocumentHeader document={document} suggestions={suggestions} />)

    expect(screen.getByText(/Original/)).toBeInTheDocument()
    expect(screen.getByText(/With accepted/)).toBeInTheDocument()
    expect(screen.getAllByText('11')).toHaveLength(2)
  })
})
