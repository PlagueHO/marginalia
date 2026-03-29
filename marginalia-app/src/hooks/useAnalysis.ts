import { useState, useCallback } from "react";
import type { Suggestion, AnalyzeRequest } from "@/types";
import * as documentService from "@/services/documentService";

interface UseAnalysisState {
  isAnalyzing: boolean;
  progress: string;
  error: string | null;
}

export function useAnalysis() {
  const [state, setState] = useState<UseAnalysisState>({
    isAnalyzing: false,
    progress: "",
    error: null,
  });

  const analyze = useCallback(
    async (request: AnalyzeRequest): Promise<Suggestion[]> => {
      setState({ isAnalyzing: true, progress: "Analyzing document…", error: null });
      try {
        const response = await documentService.analyzeDocument(request);
        setState({
          isAnalyzing: false,
          progress: "Analysis complete",
          error: null,
        });
        return response;
      } catch (err) {
        const message =
          err instanceof Error ? err.message : "Analysis failed";
        setState({ isAnalyzing: false, progress: "", error: message });
        throw err;
      }
    },
    []
  );

  const clearError = useCallback(() => {
    setState((prev) => ({ ...prev, error: null }));
  }, []);

  return {
    isAnalyzing: state.isAnalyzing,
    progress: state.progress,
    error: state.error,
    analyze,
    clearError,
  };
}
