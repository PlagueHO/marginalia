import { useState, useCallback, useMemo } from "react";
import type { Suggestion, SuggestionStatus, Paragraph } from "@/types";
import * as suggestionService from "@/services/suggestionService";

interface UseSuggestionsState {
  suggestions: Suggestion[];
  paragraphs: Paragraph[];
  isLoading: boolean;
  error: string | null;
  filter: SuggestionStatus | "All";
  activeSuggestionId: string | null;
  hoveredSuggestionId: string | null;
}

export function useSuggestions() {
  const [state, setState] = useState<UseSuggestionsState>({
    suggestions: [],
    paragraphs: [],
    isLoading: false,
    error: null,
    filter: "All",
    activeSuggestionId: null,
    hoveredSuggestionId: null,
  });

  const setSuggestions = useCallback((suggestions: Suggestion[]) => {
    setState((prev) => ({ ...prev, suggestions }));
  }, []);

  const setParagraphs = useCallback((paragraphs: Paragraph[]) => {
    setState((prev) => ({ ...prev, paragraphs }));
  }, []);

  const setFilter = useCallback((filter: SuggestionStatus | "All") => {
    setState((prev) => ({ ...prev, filter }));
  }, []);

  const setActiveSuggestion = useCallback((id: string | null) => {
    setState((prev) => ({ ...prev, activeSuggestionId: id }));
  }, []);

  const setHoveredSuggestion = useCallback((id: string | null) => {
    setState((prev) => ({ ...prev, hoveredSuggestionId: id }));
  }, []);

  const suggestionNumbers = useMemo(() => {
    // Build a paragraph index map for ordering
    const paragraphOrder = new Map<string, number>();
    state.paragraphs.forEach((p, i) => paragraphOrder.set(p.id, i));

    const sorted = [...state.suggestions].sort((a, b) => {
      const aIdx = paragraphOrder.get(a.paragraphId) ?? Infinity;
      const bIdx = paragraphOrder.get(b.paragraphId) ?? Infinity;
      return aIdx - bIdx;
    });
    const map = new Map<string, number>();
    sorted.forEach((s, i) => map.set(s.id, i + 1));
    return map;
  }, [state.suggestions, state.paragraphs]);

  const sortedSuggestions = useMemo(() => {
    const paragraphOrder = new Map<string, number>();
    state.paragraphs.forEach((p, i) => paragraphOrder.set(p.id, i));

    return [...state.suggestions].sort((a, b) => {
      const aIdx = paragraphOrder.get(a.paragraphId) ?? Infinity;
      const bIdx = paragraphOrder.get(b.paragraphId) ?? Infinity;

      if (aIdx !== bIdx) {
        return aIdx - bIdx;
      }

      return a.id.localeCompare(b.id);
    });
  }, [state.suggestions, state.paragraphs]);

  const filteredSuggestions = useMemo(() => {
    if (state.filter === "All") {
      return sortedSuggestions;
    }

    return sortedSuggestions.filter((s) => s.status === state.filter);
  }, [sortedSuggestions, state.filter]);

  const updateStatus = useCallback(
    async (
      documentId: string,
      suggestionId: string,
      status: SuggestionStatus,
      modifiedText?: string
    ) => {
      try {
        const updated = await suggestionService.updateSuggestionStatus(
          documentId,
          suggestionId,
          { status, userSteeringInput: modifiedText }
        );
        setState((prev) => ({
          ...prev,
          suggestions: prev.suggestions.map((s) =>
            s.id === suggestionId ? updated : s
          ),
        }));
        return updated;
      } catch (err) {
        const message =
          err instanceof Error
            ? err.message
            : "Failed to update suggestion";
        setState((prev) => ({ ...prev, error: message }));
        throw err;
      }
    },
    []
  );

  const acceptAll = useCallback(
    async (documentId: string) => {
      const pending = state.suggestions.filter(
        (s) => s.status === "Pending"
      );
      for (const suggestion of pending) {
        await updateStatus(documentId, suggestion.id, "Accepted");
      }
    },
    [state.suggestions, updateStatus]
  );

  const rejectAll = useCallback(
    async (documentId: string) => {
      const pending = state.suggestions.filter(
        (s) => s.status === "Pending"
      );
      for (const suggestion of pending) {
        await updateStatus(documentId, suggestion.id, "Rejected");
      }
    },
    [state.suggestions, updateStatus]
  );

  const counts = useMemo(() => {
    const result = { Pending: 0, Accepted: 0, Rejected: 0, Modified: 0, total: 0 };
    for (const s of sortedSuggestions) {
      result[s.status]++;
      result.total++;
    }
    return result;
  }, [sortedSuggestions]);

  return {
    suggestions: sortedSuggestions,
    paragraphs: state.paragraphs,
    filteredSuggestions,
    isLoading: state.isLoading,
    error: state.error,
    filter: state.filter,
    activeSuggestionId: state.activeSuggestionId,
    hoveredSuggestionId: state.hoveredSuggestionId,
    suggestionNumbers,
    counts,
    setSuggestions,
    setParagraphs,
    setFilter,
    setActiveSuggestion,
    setHoveredSuggestion,
    updateStatus,
    acceptAll,
    rejectAll,
  };
}
