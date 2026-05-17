import { useState, useCallback } from "react";
import type { AccessControlStatus, LlmHealthResult } from "@/types";
import * as configService from "@/services/configService";

interface UseLlmConfigState {
  isCheckingHealth: boolean;
  healthResult: LlmHealthResult | null;
  isCheckingAccess: boolean;
  accessStatus: AccessControlStatus | null;
  error: string | null;
}

export function useLlmConfig() {
  const [state, setState] = useState<UseLlmConfigState>({
    isCheckingHealth: false,
    healthResult: null,
    isCheckingAccess: false,
    accessStatus: null,
    error: null,
  });

  const checkHealth = useCallback(async () => {
    setState((prev) => ({
      ...prev,
      isCheckingHealth: true,
      isCheckingAccess: true,
      healthResult: null,
      accessStatus: null,
      error: null,
    }));

    await Promise.allSettled([
      configService.checkHealth().then(
        (result) => setState((prev) => ({ ...prev, isCheckingHealth: false, healthResult: result })),
        (err) =>
          setState((prev) => ({
            ...prev,
            isCheckingHealth: false,
            healthResult: { healthy: false, message: err instanceof Error ? err.message : "Health check failed" },
          })),
      ),
      configService.getAccessStatus().then(
        (result) => setState((prev) => ({ ...prev, isCheckingAccess: false, accessStatus: result })),
        () => setState((prev) => ({ ...prev, isCheckingAccess: false })),
      ),
    ]);
  }, []);

  return {
    isCheckingHealth: state.isCheckingHealth,
    healthResult: state.healthResult,
    isCheckingAccess: state.isCheckingAccess,
    accessStatus: state.accessStatus,
    error: state.error,
    checkHealth,
  };
}
