import { useState, useCallback, useEffect } from "react";
import type { LlmConfig, LlmHealthResult } from "@/types";
import * as configService from "@/services/configService";

interface UseLlmConfigState {
  config: LlmConfig;
  isLoading: boolean;
  isCheckingHealth: boolean;
  healthResult: LlmHealthResult | null;
  error: string | null;
}

export function useLlmConfig() {
  const [state, setState] = useState<UseLlmConfigState>({
    config: {},
    isLoading: false,
    isCheckingHealth: false,
    healthResult: null,
    error: null,
  });

  const loadConfig = useCallback(async () => {
    setState((prev) => ({ ...prev, isLoading: true, error: null }));
    try {
      const config = await configService.getLlmConfig();
      setState((prev) => ({ ...prev, config, isLoading: false }));
    } catch (err) {
      const message =
        err instanceof Error ? err.message : "Failed to load config";
      setState((prev) => ({ ...prev, isLoading: false, error: message }));
    }
  }, []);

  useEffect(() => {
    let cancelled = false;
    configService.getLlmConfig().then(
      (config) => {
        if (!cancelled) setState((prev) => ({ ...prev, config, isLoading: false }));
      },
      (err) => {
        if (!cancelled) {
          const message = err instanceof Error ? err.message : "Failed to load config";
          setState((prev) => ({ ...prev, isLoading: false, error: message }));
        }
      }
    );
    return () => { cancelled = true; };
  }, []);

  const checkHealth = useCallback(async () => {
    setState((prev) => ({
      ...prev,
      isCheckingHealth: true,
      healthResult: null,
      error: null,
    }));
    try {
      const result = await configService.checkHealth();
      setState((prev) => ({
        ...prev,
        isCheckingHealth: false,
        healthResult: result,
      }));
    } catch (err) {
      const message =
        err instanceof Error ? err.message : "Health check failed";
      setState((prev) => ({
        ...prev,
        isCheckingHealth: false,
        healthResult: { healthy: false, message },
      }));
    }
  }, []);

  return {
    config: state.config,
    isLoading: state.isLoading,
    isCheckingHealth: state.isCheckingHealth,
    healthResult: state.healthResult,
    error: state.error,
    loadConfig,
    checkHealth,
  };
}
