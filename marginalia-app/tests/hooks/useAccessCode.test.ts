import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { renderHook, act, waitFor } from '@testing-library/react'
import { useAccessCode } from '@/hooks/useAccessCode'

const mockFetch = vi.fn()

// Mock configService.getAccessStatus
vi.mock('@/services/configService', () => ({
  getAccessStatus: vi.fn(),
}))

// Mock api.setAccessCode and api.getApiBaseUrl
vi.mock('@/services/api', () => ({
  setAccessCode: vi.fn(),
  getApiBaseUrl: vi.fn(() => 'http://localhost:5279'),
}))

import { getAccessStatus } from '@/services/configService'
import { setAccessCode } from '@/services/api'

const mockGetAccessStatus = vi.mocked(getAccessStatus)
const mockSetAccessCode = vi.mocked(setAccessCode)

beforeEach(() => {
  vi.clearAllMocks()
  mockFetch.mockReset()
  vi.stubGlobal('fetch', mockFetch)
  sessionStorage.clear()
})

afterEach(() => {
  vi.restoreAllMocks()
})

describe('useAccessCode', () => {
  describe('when access code is not required', () => {
    it('sets isVerified to true and isLoading to false', async () => {
      mockGetAccessStatus.mockResolvedValueOnce({ accessCodeRequired: false })

      const { result } = renderHook(() => useAccessCode())

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      expect(result.current.accessCodeRequired).toBe(false)
      expect(result.current.isVerified).toBe(true)
      expect(result.current.error).toBeNull()
    })
  })

  describe('when access code is required', () => {
    it('sets accessCodeRequired to true', async () => {
      mockGetAccessStatus.mockResolvedValueOnce({ accessCodeRequired: true })

      const { result } = renderHook(() => useAccessCode())

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      expect(result.current.accessCodeRequired).toBe(true)
      expect(result.current.isVerified).toBe(false)
    })

    it('validates cached sessionStorage code on mount', async () => {
      sessionStorage.setItem('accessCode', 'cached-code')
      mockGetAccessStatus.mockResolvedValueOnce({ accessCodeRequired: true })
      mockFetch.mockResolvedValueOnce({ ok: true })

      const { result } = renderHook(() => useAccessCode())

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      expect(mockSetAccessCode).toHaveBeenCalledWith('cached-code')
      expect(result.current.isVerified).toBe(true)
    })

    it('clears invalid cached code', async () => {
      sessionStorage.setItem('accessCode', 'bad-code')
      mockGetAccessStatus.mockResolvedValueOnce({ accessCodeRequired: true })
      mockFetch.mockResolvedValueOnce({ ok: false, status: 401 })

      const { result } = renderHook(() => useAccessCode())

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      expect(result.current.isVerified).toBe(false)
      expect(sessionStorage.getItem('accessCode')).toBeNull()
      expect(mockSetAccessCode).toHaveBeenCalledWith(null)
    })
  })

  describe('submitCode', () => {
    it('sets isVerified on valid code', async () => {
      mockGetAccessStatus.mockResolvedValueOnce({ accessCodeRequired: true })

      const { result } = renderHook(() => useAccessCode())

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      mockFetch.mockResolvedValueOnce({ ok: true })

      await act(async () => {
        await result.current.submitCode('valid-code')
      })

      expect(result.current.isVerified).toBe(true)
      expect(result.current.error).toBeNull()
      expect(sessionStorage.getItem('accessCode')).toBe('valid-code')
    })

    it('sets error on invalid code', async () => {
      mockGetAccessStatus.mockResolvedValueOnce({ accessCodeRequired: true })

      const { result } = renderHook(() => useAccessCode())

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      mockFetch.mockResolvedValueOnce({ ok: false, status: 401 })

      await act(async () => {
        await result.current.submitCode('wrong-code')
      })

      expect(result.current.isVerified).toBe(false)
      expect(result.current.error).toBe('Invalid access code')
      expect(mockSetAccessCode).toHaveBeenCalledWith(null)
    })

    it('sets error on network failure', async () => {
      mockGetAccessStatus.mockResolvedValueOnce({ accessCodeRequired: true })

      const { result } = renderHook(() => useAccessCode())

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      mockFetch.mockRejectedValueOnce(new TypeError('Failed to fetch'))

      await act(async () => {
        await result.current.submitCode('some-code')
      })

      expect(result.current.isVerified).toBe(false)
      expect(result.current.error).toBe('Unable to verify access code. Please try again.')
    })
  })

  describe('when server is unreachable', () => {
    it('allows through if status check fails', async () => {
      mockGetAccessStatus.mockRejectedValueOnce(new Error('Network error'))

      const { result } = renderHook(() => useAccessCode())

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false)
      })

      expect(result.current.isVerified).toBe(true)
    })
  })
})
