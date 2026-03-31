import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import {
  apiGet,
  apiPost,
  apiPut,
  apiPostFile,
  apiGetBlob,
  setApiBaseUrl,
  getApiBaseUrl,
} from '@/services/api'
import { uploadDocument, pasteDocument, analyzeDocument } from '@/services/documentService'
import { getLlmConfig, checkHealth } from '@/services/configService'
import { updateSuggestionStatus } from '@/services/suggestionService'

// Mock global fetch
const mockFetch = vi.fn()

beforeEach(() => {
  mockFetch.mockReset()
  vi.stubGlobal('fetch', mockFetch)
  setApiBaseUrl('http://localhost:5279')
})

afterEach(() => {
  vi.restoreAllMocks()
})

describe('API base client', () => {
  describe('setApiBaseUrl / getApiBaseUrl', () => {
    it('returns default base URL', () => {
      setApiBaseUrl('http://localhost:5279')
      expect(getApiBaseUrl()).toBe('http://localhost:5279')
    })

    it('strips trailing slashes from base URL', () => {
      setApiBaseUrl('http://localhost:5279///')
      expect(getApiBaseUrl()).toBe('http://localhost:5279')
    })
  })

  describe('apiGet', () => {
    it('makes GET request to correct URL', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve({ id: 'doc-1' }),
      })

      await apiGet('/api/documents/doc-1')

      expect(mockFetch).toHaveBeenCalledWith(
        'http://localhost:5279/api/documents/doc-1',
        expect.objectContaining({ method: 'GET' })
      )
    })

    it('throws ApiError on non-ok response', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 404,
        statusText: 'Not Found',
        json: () => Promise.reject(new Error('no body')),
      })

      await expect(apiGet('/api/documents/nonexistent')).rejects.toEqual(
        expect.objectContaining({
          message: 'Not Found',
          statusCode: 404,
        })
      )
    })

    it('uses error message from response body when available', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 400,
        statusText: 'Bad Request',
        json: () => Promise.resolve({ message: 'Document ID is required' }),
      })

      await expect(apiGet('/api/documents/')).rejects.toEqual(
        expect.objectContaining({
          message: 'Document ID is required',
        })
      )
    })
  })

  describe('apiPost', () => {
    it('makes POST request with JSON body', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve({ suggestions: [] }),
      })

      await apiPost('/api/documents/analyze', {
        documentId: 'doc-1',
        content: 'text',
      })

      expect(mockFetch).toHaveBeenCalledWith(
        'http://localhost:5279/api/documents/analyze',
        expect.objectContaining({
          method: 'POST',
          headers: { 'Content-Type': 'application/json', 'X-User-Id': '_anonymous' },
          body: JSON.stringify({ documentId: 'doc-1', content: 'text' }),
        })
      )
    })

    it('sends POST without body when body is undefined', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve({}),
      })

      await apiPost('/api/endpoint')

      expect(mockFetch).toHaveBeenCalledWith(
        'http://localhost:5279/api/endpoint',
        expect.objectContaining({
          method: 'POST',
          body: undefined,
        })
      )
    })
  })

  describe('apiPut', () => {
    it('makes PUT request with JSON body', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve({}),
      })

      await apiPut('/api/config/llm', { endpoint: 'https://new.com' })

      expect(mockFetch).toHaveBeenCalledWith(
        'http://localhost:5279/api/config/llm',
        expect.objectContaining({
          method: 'PUT',
          body: JSON.stringify({ endpoint: 'https://new.com' }),
        })
      )
    })
  })

  describe('apiPostFile', () => {
    it('sends file as multipart form data', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: () => Promise.resolve({ document: { id: 'doc-1' } }),
      })

      const file = new File(['content'], 'manuscript.docx', {
        type: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      })

      await apiPostFile('/api/documents/upload', file)

      const [url, options] = mockFetch.mock.calls[0] as [string, RequestInit]
      expect(url).toBe('http://localhost:5279/api/documents/upload')
      expect(options.method).toBe('POST')
      expect(options.body).toBeInstanceOf(FormData)

      const formData = options.body as FormData
      expect(formData.get('file')).toBe(file)
    })
  })

  describe('apiGetBlob', () => {
    it('returns blob for download', async () => {
      const mockBlob = new Blob(['file content'], { type: 'application/octet-stream' })
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        blob: () => Promise.resolve(mockBlob),
      })

      const result = await apiGetBlob('/api/documents/doc-1/export')

      expect(result).toBe(mockBlob)
    })

    it('throws on download failure', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
        statusText: 'Internal Server Error',
      })

      await expect(apiGetBlob('/api/documents/doc-1/export')).rejects.toEqual(
        expect.objectContaining({
          message: 'Internal Server Error',
          statusCode: 500,
        })
      )
    })
  })

  describe('204 No Content handling', () => {
    it('returns undefined for 204 responses', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 204,
        json: () => Promise.reject(new Error('no body')),
      })

      const result = await apiPut('/api/some-endpoint', {})
      expect(result).toBeUndefined()
    })
  })

  describe('network error handling', () => {
    it('propagates network errors', async () => {
      mockFetch.mockRejectedValueOnce(new TypeError('Failed to fetch'))

      await expect(apiGet('/api/documents/doc-1')).rejects.toThrow('Failed to fetch')
    })
  })
})

describe('Document service', () => {
  it('uploadDocument calls apiPostFile with correct path', async () => {
    const file = new File(['content'], 'test.docx')
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () =>
        Promise.resolve({
          document: { id: 'doc-1', filename: 'test.docx' },
          sessionId: 'session-1',
        }),
    })

    const result = await uploadDocument(file)

    expect(mockFetch).toHaveBeenCalledWith(
      'http://localhost:5279/api/documents/upload',
      expect.objectContaining({ method: 'POST' })
    )
    expect(result.document.id).toBe('doc-1')
  })

  it('pasteDocument sends content as JSON', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () =>
        Promise.resolve({
          document: { id: 'doc-2', filename: 'pasted.txt' },
          sessionId: 'session-1',
        }),
    })

    await pasteDocument({ content: 'pasted text' })

    expect(mockFetch).toHaveBeenCalledWith(
      'http://localhost:5279/api/documents/paste',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ content: 'pasted text' }),
      })
    )
  })

  it('analyzeDocument sends analysis request', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ suggestions: [] }),
    })

    await analyzeDocument({
      documentId: 'doc-1',
      content: 'test content',
      userGuidance: 'more narrative',
    })

    expect(mockFetch).toHaveBeenCalledWith(
      'http://localhost:5279/api/documents/doc-1/analyze',
      expect.objectContaining({
        method: 'POST',
        body: expect.stringContaining('more narrative'),
      })
    )
  })
})

describe('Config service', () => {
  it('getLlmConfig fetches from correct endpoint', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () =>
        Promise.resolve({
          endpoint: 'https://foundry.azure.com',
          modelName: 'gpt-4o',
        }),
    })

    const config = await getLlmConfig()

    expect(mockFetch).toHaveBeenCalledWith(
      'http://localhost:5279/api/config/llm',
      expect.objectContaining({ method: 'GET' })
    )
    expect(config.endpoint).toBe('https://foundry.azure.com')
  })

  it('checkHealth fetches from health endpoint', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () =>
        Promise.resolve({ healthy: true, message: 'Connected to Azure AI Foundry via Entra ID' }),
    })

    const result = await checkHealth()

    expect(mockFetch).toHaveBeenCalledWith(
      'http://localhost:5279/api/config/llm/health',
      expect.objectContaining({ method: 'GET' })
    )
    expect(result.healthy).toBe(true)
    expect(result.message).toBe('Connected to Azure AI Foundry via Entra ID')
  })
})

describe('Suggestion service', () => {
  it('updateSuggestionStatus sends PUT with status', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () =>
        Promise.resolve({ id: 'sug-1', status: 'Accepted' }),
    })

    await updateSuggestionStatus('doc-1', 'sug-1', {
      status: 'Accepted',
    })

    expect(mockFetch).toHaveBeenCalledWith(
      'http://localhost:5279/api/documents/doc-1/suggestions/sug-1',
      expect.objectContaining({
        method: 'PUT',
        body: JSON.stringify({ status: 'Accepted' }),
      })
    )
  })
})
