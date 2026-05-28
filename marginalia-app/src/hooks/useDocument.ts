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

  const uploadFile = useCallback(async (file: File, title?: string) => {
    setState((prev) => ({ ...prev, isLoading: true, error: null }));
    try {
      const response = await documentService.uploadDocument(file, title);
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
    async (content: string, filename?: string, title?: string) => {
      setState((prev) => ({ ...prev, isLoading: true, error: null }));
      try {
        const response = await documentService.pasteDocument({
          content,
          filename,
          title,
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

  const renameDocument = useCallback(async (title: string) => {
    setState((prev) => ({ ...prev, isLoading: true, error: null }));
    try {
      const id = state.document?.id;
      if (!id) throw new Error("No document loaded");
      const updated = await documentService.renameDocument(id, title);
      setState({ document: updated, isLoading: false, error: null });
    } catch (err) {
      const message =
        err instanceof Error ? err.message : "Failed to rename document";
      setState((prev) => ({ ...prev, isLoading: false, error: message }));
      throw err;
    }
  }, [state.document]);

  const deleteDocument = useCallback(async () => {
    setState((prev) => ({ ...prev, isLoading: true, error: null }));
    try {
      const id = state.document?.id;
      if (!id) throw new Error("No document loaded");
      await documentService.deleteDocument(id);
      setState({ document: null, isLoading: false, error: null });
    } catch (err) {
      const message =
        err instanceof Error ? err.message : "Failed to delete document";
      setState((prev) => ({ ...prev, isLoading: false, error: message }));
      throw err;
    }
  }, [state.document]);

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
    renameDocument,
    deleteDocument,
    clearDocument,
  };
}
