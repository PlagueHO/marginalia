import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { MemoryRouter } from 'react-router-dom'
import { TooltipProvider } from '@/components/ui/tooltip'
import { HomePage } from '@/pages/HomePage'

/**
 * Tests for the Home page component.
 *
 * The Home page:
 * - Lists all documents for the current user via GET /api/documents
 * - Shows DocumentSummary cards (title, status, date, suggestion count)
 * - Supports empty state, loading state, and error state
 * - Uses react-router-dom useNavigate for navigation
 * - Has a "New Manuscript" button for creating new documents
 */

const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

// Mock fetch at the network boundary
const mockFetch = vi.fn()

// Default mock for LLM config endpoint (used by AppHeader via useLlmConfig)
function mockConfigResponse() {
  return {
    ok: true,
    status: 200,
    json: () => Promise.resolve({ endpoint: null, modelName: null }),
  }
}

beforeEach(() => {
  mockFetch.mockReset()
  mockNavigate.mockReset()
  vi.stubGlobal('fetch', mockFetch)
})

afterEach(() => {
  vi.restoreAllMocks()
})

const mockDocuments = [
  {
    id: 'doc-1',
    title: '2026-03-29 10:15 - Chapter 1 Draft.docx',
    filename: 'Chapter 1 Draft.docx',
    source: 'Local',
    status: 'Analyzed',
    createdAt: '2026-03-29T10:15:00Z',
    updatedAt: '2026-03-29T14:30:00Z',
    suggestionCount: 12,
  },
  {
    id: 'doc-2',
    title: 'My Research Notes',
    filename: 'pasted-text.txt',
    source: 'Local',
    status: 'Draft',
    createdAt: '2026-03-28T09:00:00Z',
    updatedAt: '2026-03-28T09:00:00Z',
    suggestionCount: 0,
  },
]

function mockFetchDocuments(documents = mockDocuments) {
  // Mock both the config fetch (from useLlmConfig) and the documents fetch
  mockFetch.mockImplementation((url: string) => {
    if (url.includes('/api/config/llm')) {
      return Promise.resolve(mockConfigResponse())
    }
    return Promise.resolve({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ documents }),
    })
  })
}

function mockFetchError(status = 500, message = 'Internal Server Error') {
  mockFetch.mockImplementation((url: string) => {
    if (url.includes('/api/config/llm')) {
      return Promise.resolve(mockConfigResponse())
    }
    return Promise.resolve({
      ok: false,
      status,
      statusText: message,
      json: () => Promise.resolve({ message }),
    })
  })
}

describe('HomePage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  function renderHomePage() {
    return render(
      <MemoryRouter>
        <TooltipProvider>
          <HomePage />
        </TooltipProvider>
      </MemoryRouter>
    )
  }

  describe('document list rendering', () => {
    it('renders document list when documents exist', async () => {
      mockFetchDocuments()
      renderHomePage()

      expect(await screen.findByText(/chapter 1 draft/i)).toBeInTheDocument()
      expect(screen.getByText(/my research notes/i)).toBeInTheDocument()
    })

    it('each document card shows title', async () => {
      mockFetchDocuments()
      renderHomePage()

      expect(await screen.findByText(/chapter 1 draft/i)).toBeInTheDocument()
      expect(screen.getByText(/my research notes/i)).toBeInTheDocument()
    })

    it('each document card shows status', async () => {
      mockFetchDocuments()
      renderHomePage()

      await screen.findByText(/chapter 1 draft/i)

      expect(screen.getByText('Analyzed')).toBeInTheDocument()
      expect(screen.getByText('Draft')).toBeInTheDocument()
    })

    it('each document card shows suggestion count', async () => {
      mockFetchDocuments()
      renderHomePage()

      await screen.findByText(/chapter 1 draft/i)

      // doc-1 has 12 suggestions
      expect(screen.getByText(/12/)).toBeInTheDocument()
    })

    it('each document card shows date information', async () => {
      mockFetchDocuments()
      renderHomePage()

      await screen.findByText(/chapter 1 draft/i)

      // Both documents show dates containing 2026
      const dateElements = screen.getAllByText(/2026/)
      expect(dateElements.length).toBeGreaterThanOrEqual(1)
    })
  })

  describe('empty state', () => {
    it('shows empty state message when no documents exist', async () => {
      mockFetchDocuments([])
      renderHomePage()

      expect(
        await screen.findByText(/no (documents|manuscripts)/i)
      ).toBeInTheDocument()
    })
  })

  describe('loading state', () => {
    it('shows loading indicator while fetching documents', () => {
      // Config endpoint returns immediately, documents fetch stays pending
      mockFetch.mockImplementation((url: string) => {
        if (url.includes('/api/config/llm')) {
          return Promise.resolve(mockConfigResponse())
        }
        return new Promise(() => {})
      })
      renderHomePage()

      expect(screen.getByText(/loading/i)).toBeInTheDocument()
    })
  })

  describe('error state', () => {
    it('shows error message on API failure', async () => {
      mockFetchError(500, 'Internal Server Error')
      renderHomePage()

      expect(
        await screen.findByText(/error|failed|unable/i)
      ).toBeInTheDocument()
    })
  })

  describe('navigation', () => {
    it('clicking a document navigates to editor page', async () => {
      const user = userEvent.setup()
      mockFetchDocuments()
      renderHomePage()

      const firstDoc = await screen.findByText(/chapter 1 draft/i)
      await user.click(firstDoc)

      expect(mockNavigate).toHaveBeenCalledWith('/editor/doc-1')
    })

    it('"New Manuscript" button navigates to new page', async () => {
      const user = userEvent.setup()
      mockFetchDocuments()
      renderHomePage()

      await screen.findByText(/chapter 1 draft/i)

      const newButton = screen.getByRole('button', { name: /new manuscript/i })
      await user.click(newButton)

      expect(mockNavigate).toHaveBeenCalledWith('/new')
    })
  })
})
