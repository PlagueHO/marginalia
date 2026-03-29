import { useState, useCallback } from "react";
import type { Document } from "@/types";
import * as documentService from "@/services/documentService";

interface UseDocumentState {
  document: Document | null;
  isLoading: boolean;
  error: string | null;
}

export function useDocument() {
  const [state, setState] = useState<UseDocumentState>({
    document: null,
    isLoading: false,
    error: null,
  });

  const uploadFile = useCallback(async (file: File) => {
    setState((prev) => ({ ...prev, isLoading: true, error: null }));
    try {
      const response = await documentService.uploadDocument(file);
      setState({
        document: response.document,
        isLoading: false,
        error: null,
      });
      return response;
    } catch (err) {
      const message =
        err instanceof Error ? err.message : "Failed to upload document";
      setState((prev) => ({ ...prev, isLoading: false, error: message }));
      throw err;
    }
  }, []);

  const pasteContent = useCallback(
    async (content: string, filename?: string) => {
      setState((prev) => ({ ...prev, isLoading: true, error: null }));
      try {
        const response = await documentService.pasteDocument({
          content,
          filename,
        });
        setState({
          document: response.document,
          isLoading: false,
          error: null,
        });
        return response;
      } catch (err) {
        const message =
          err instanceof Error ? err.message : "Failed to process text";
        setState((prev) => ({ ...prev, isLoading: false, error: message }));
        throw err;
      }
    },
    []
  );

  const loadDocument = useCallback(async (documentId: string) => {
    setState((prev) => ({ ...prev, isLoading: true, error: null }));
    try {
      const doc = await documentService.getDocument(documentId);
      setState({ document: doc, isLoading: false, error: null });
      return doc;
    } catch (err) {
      const message =
        err instanceof Error ? err.message : "Failed to load document";
      setState((prev) => ({ ...prev, isLoading: false, error: message }));
      throw err;
    }
  }, []);

  const updateDocument = useCallback((doc: Document) => {
    setState({ document: doc, isLoading: false, error: null });
  }, []);

  const clearDocument = useCallback(() => {
    setState({ document: null, isLoading: false, error: null });
  }, []);

  return {
    document: state.document,
    isLoading: state.isLoading,
    error: state.error,
    uploadFile,
    pasteContent,
    loadDocument,
    updateDocument,
    clearDocument,
  };
}
