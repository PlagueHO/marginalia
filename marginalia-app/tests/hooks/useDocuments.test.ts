import { renderHook, act } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { useDocuments } from '@/hooks/useDocuments'
import { setApiBaseUrl } from '@/services/api'

/**
 * Tests for the useDocuments hook (document listing for the home page).
 *
 * The hook exposes loadDocuments() which must be called explicitly
 * (the consuming component calls it in useEffect).
 */

const mockFetch = vi.fn()

beforeEach(() => {
  mockFetch.mockReset()
  vi.stubGlobal('fetch', mockFetch)
  setApiBaseUrl('http://localhost:5279')
})

afterEach(() => {
  vi.restoreAllMocks()
})

const mockDocumentList = {
  documents: [
    {
      id: 'doc-1',
      title: 'Chapter 1',
      filename: 'chapter1.docx',
      source: 'Local',
      status: 'Analyzed',
      createdAt: '2026-03-29T10:15:00Z',
      updatedAt: '2026-03-29T14:30:00Z',
      suggestionCount: 12,
    },
    {
      id: 'doc-2',
      title: 'Research Notes',
      filename: 'pasted-text.txt',
      source: 'Local',
      status: 'Draft',
      createdAt: '2026-03-28T09:00:00Z',
      updatedAt: '2026-03-28T09:00:00Z',
      suggestionCount: 0,
    },
  ],
}

describe('useDocuments', () => {
  it('fetches documents when loadDocuments is called', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve(mockDocumentList),
    })

    const { result } = renderHook(() => useDocuments())

    await act(async () => {
      await result.current.loadDocuments()
    })

    expect(result.current.documents).toHaveLength(2)
    expect(result.current.documents[0].id).toBe('doc-1')
    expect(result.current.documents[1].id).toBe('doc-2')
  })

  it('sets loading state while fetching', async () => {
    // Return a promise that never resolves to keep loading state active
    mockFetch.mockReturnValueOnce(new Promise(() => {}))

    const { result } = renderHook(() => useDocuments())

    act(() => {
      result.current.loadDocuments()
    })

    expect(result.current.isLoading).toBe(true)
    expect(result.current.documents).toEqual([])
  })

  it('handles API error gracefully', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
      statusText: 'Internal Server Error',
      json: () => Promise.reject(new Error('no body')),
    })

    const { result } = renderHook(() => useDocuments())

    await act(async () => {
      await result.current.loadDocuments()
    })

    expect(result.current.error).toBeTruthy()
    expect(result.current.documents).toEqual([])
  })

  it('returns empty documents array when API returns empty list', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ documents: [] }),
    })

    const { result } = renderHook(() => useDocuments())

    await act(async () => {
      await result.current.loadDocuments()
    })

    expect(result.current.documents).toEqual([])
    expect(result.current.error).toBeNull()
  })

  it('calls the correct API endpoint', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve(mockDocumentList),
    })

    const { result } = renderHook(() => useDocuments())

    await act(async () => {
      await result.current.loadDocuments()
    })

    expect(mockFetch).toHaveBeenCalledWith(
      'http://localhost:5279/api/documents',
      expect.objectContaining({
        method: 'GET',
        headers: expect.objectContaining({
          'X-User-Id': expect.any(String),
        }),
      })
    )
  })
})
