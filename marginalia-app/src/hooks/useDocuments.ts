import { useState, useCallback } from "react";
import type { DocumentSummary } from "@/types";
import * as documentService from "@/services/documentService";

interface UseDocumentsState {
  documents: DocumentSummary[];
  isLoading: boolean;
  error: string | null;
}

export function useDocuments() {
  const [state, setState] = useState<UseDocumentsState>({
    documents: [],
    isLoading: false,
    error: null,
  });

  const loadDocuments = useCallback(async () => {
    setState((prev) => ({ ...prev, isLoading: true, error: null }));
    try {
      const response = await documentService.listDocuments();
      setState({
        documents: response.documents,
        isLoading: false,
        error: null,
      });
    } catch (err) {
      const message =
        err instanceof Error ? err.message : "Failed to load documents";
      setState((prev) => ({ ...prev, isLoading: false, error: message }));
    }
  }, []);

  return {
    documents: state.documents,
    isLoading: state.isLoading,
    error: state.error,
    loadDocuments,
  };
}
