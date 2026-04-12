import { useState, useCallback } from "react";
import type { Suggestion, AnalyzeRequest, Document } from "@/types";
import * as documentService from "@/services/documentService";

interface UseAnalysisState {
  isAnalyzing: boolean;
  progress: string;
  error: string | null;
  showConfirmReplaceDialog: boolean;
  acceptedSuggestions: Suggestion[];
  pendingCount: number;
}

export function useAnalysis() {
  const [state, setState] = useState<UseAnalysisState>({
    isAnalyzing: false,
    progress: "",
    error: null,
    showConfirmReplaceDialog: false,
    acceptedSuggestions: [],
    pendingCount: 0,
  });

  const analyze = useCallback(
    async (request: AnalyzeRequest): Promise<Suggestion[]> => {
      setState((prev) => ({
        ...prev,
        isAnalyzing: true,
        progress: "Analyzing document…",
        error: null,
      }));
      try {
        const response = await documentService.analyzeDocument(request);
        setState((prev) => ({
          ...prev,
          isAnalyzing: false,
          progress: "Analysis complete",
          error: null,
        }));
        return response;
      } catch (err) {
        const message =
          err instanceof Error ? err.message : "Analysis failed";
        setState((prev) => ({
          ...prev,
          isAnalyzing: false,
          progress: "",
          error: message,
        }));
        throw err;
      }
    },
    []
  );

  const initiateAnalysis = useCallback(
    (document: Document): "proceed" | "confirm" => {
      const normalizedStatus = document.status.toLowerCase();
      const hasExistingAnalysis =
        normalizedStatus === "analyzed" || document.suggestions.length > 0;

      if (hasExistingAnalysis) {
        const accepted = document.suggestions.filter((s) => s.status === "Accepted");
        const nonAccepted = document.suggestions.filter((s) => s.status !== "Accepted");

        setState((prev) => ({
          ...prev,
          showConfirmReplaceDialog: true,
          acceptedSuggestions: accepted,
          pendingCount: nonAccepted.length,
        }));
        return "confirm";
      }

      return "proceed";
    },
    []
  );

  const confirmReplaceAndAnalyze = useCallback(() => {
    setState((prev) => ({
      ...prev,
      showConfirmReplaceDialog: false,
    }));
  }, []);

  const cancelReplaceAnalysis = useCallback(() => {
    setState((prev) => ({
      ...prev,
      showConfirmReplaceDialog: false,
      acceptedSuggestions: [],
      pendingCount: 0,
    }));
  }, []);

  const clearError = useCallback(() => {
    setState((prev) => ({ ...prev, error: null }));
  }, []);

  return {
    isAnalyzing: state.isAnalyzing,
    progress: state.progress,
    error: state.error,
    showConfirmReplaceDialog: state.showConfirmReplaceDialog,
    acceptedSuggestions: state.acceptedSuggestions,
    pendingCount: state.pendingCount,
    analyze,
    initiateAnalysis,
    confirmReplaceAndAnalyze,
    cancelReplaceAnalysis,
    clearError,
  };
}
