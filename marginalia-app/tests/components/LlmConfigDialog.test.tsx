import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { axe } from 'jest-axe'
import { LlmConfigDialog } from '@/components/LlmConfigDialog'
import type { LlmConfig, LlmHealthResult } from '@/types'

describe('LlmConfigDialog', () => {
  const defaultConfig: LlmConfig = {
    endpoint: 'https://my-foundry.services.ai.azure.com',
    modelName: 'gpt-4o',
    authMethod: 'entraId',
    isConfigured: true,
  }

  const defaultProps = {
    open: true,
    onOpenChange: vi.fn(),
    config: defaultConfig,
    isLoading: false,
    isCheckingHealth: false,
    healthResult: null as LlmHealthResult | null,
    onCheckHealth: vi.fn().mockResolvedValue(undefined),
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('rendering', () => {
    it('renders dialog title', () => {
      render(<LlmConfigDialog {...defaultProps} />)

      expect(screen.getByText('Model Configuration')).toBeInTheDocument()
    })

    it('renders description text', () => {
      render(<LlmConfigDialog {...defaultProps} />)

      expect(
        screen.getByText(/azure ai foundry endpoint configuration/i)
      ).toBeInTheDocument()
    })

    it('does not render when open is false', () => {
      render(<LlmConfigDialog {...defaultProps} open={false} />)

      expect(screen.queryByText('Model Configuration')).not.toBeInTheDocument()
    })

    it('displays endpoint as readonly text', () => {
      render(<LlmConfigDialog {...defaultProps} />)

      const endpointDisplay = screen.getByTestId('endpoint-display')
      expect(endpointDisplay).toBeInTheDocument()
      expect(endpointDisplay).toHaveTextContent('https://my-foundry.services.ai.azure.com')
    })

    it('displays model name as readonly text', () => {
      render(<LlmConfigDialog {...defaultProps} />)

      const modelDisplay = screen.getByTestId('model-name-display')
      expect(modelDisplay).toBeInTheDocument()
      expect(modelDisplay).toHaveTextContent('gpt-4o')
    })

    it('always shows Entra ID badge', () => {
      render(<LlmConfigDialog {...defaultProps} />)

      expect(screen.getByTestId('entra-id-badge')).toBeInTheDocument()
      expect(screen.getAllByText(/entra id/i).length).toBeGreaterThan(0)
    })

    it('does not render editable inputs', () => {
      render(<LlmConfigDialog {...defaultProps} />)

      expect(screen.queryByRole('textbox')).not.toBeInTheDocument()
    })

    it('does not render Save button', () => {
      render(<LlmConfigDialog {...defaultProps} />)

      expect(screen.queryByRole('button', { name: /save/i })).not.toBeInTheDocument()
    })
  })

  describe('interactions', () => {
    it('calls onCheckHealth when Check Connection button is clicked', async () => {
      const user = userEvent.setup()
      render(<LlmConfigDialog {...defaultProps} />)

      await user.click(screen.getByRole('button', { name: /check connection/i }))

      expect(defaultProps.onCheckHealth).toHaveBeenCalledOnce()
    })

    it('disables Check Connection button when checking health', () => {
      render(<LlmConfigDialog {...defaultProps} isCheckingHealth={true} />)

      expect(
        screen.getByRole('button', { name: /check connection/i })
      ).toBeDisabled()
    })

    it('disables Check Connection button when loading', () => {
      render(<LlmConfigDialog {...defaultProps} isLoading={true} />)

      expect(
        screen.getByRole('button', { name: /check connection/i })
      ).toBeDisabled()
    })
  })

  describe('health status display', () => {
    it('shows checking spinner when isCheckingHealth is true', () => {
      render(<LlmConfigDialog {...defaultProps} isCheckingHealth={true} />)

      expect(screen.getByText(/checking connection/i)).toBeInTheDocument()
    })

    it('shows healthy status when healthResult is healthy', () => {
      const healthResult: LlmHealthResult = {
        healthy: true,
        message: 'Connected to Azure AI Foundry via Entra ID',
      }
      render(<LlmConfigDialog {...defaultProps} healthResult={healthResult} />)

      const status = screen.getByTestId('health-status')
      expect(status).toBeInTheDocument()
      expect(status).toHaveTextContent('Connected')
      expect(status).toHaveTextContent('Connected to Azure AI Foundry via Entra ID')
    })

    it('shows unhealthy status when healthResult is not healthy', () => {
      const healthResult: LlmHealthResult = {
        healthy: false,
        message: 'Unable to reach endpoint',
      }
      render(<LlmConfigDialog {...defaultProps} healthResult={healthResult} />)

      const status = screen.getByTestId('health-status')
      expect(status).toBeInTheDocument()
      expect(screen.getByText(/disconnected/i)).toBeInTheDocument()
      expect(screen.getByText(/unable to reach endpoint/i)).toBeInTheDocument()
    })

    it('shows "Not checked" when healthResult is null and not checking', () => {
      render(<LlmConfigDialog {...defaultProps} healthResult={null} />)

      expect(screen.getByText(/not checked/i)).toBeInTheDocument()
    })
  })

  describe('accessibility', () => {
    it('passes axe accessibility checks', async () => {
      const { container } = render(<LlmConfigDialog {...defaultProps} />)

      const results = await axe(container)
      expect(results).toHaveNoViolations()
    })
  })
})
