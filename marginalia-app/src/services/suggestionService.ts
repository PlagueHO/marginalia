import type { Suggestion, SuggestionUpdateRequest } from "@/types";
import { apiGet, apiPut } from "./api";

export async function getSuggestions(
  documentId: string
): Promise<Suggestion[]> {
  return apiGet<Suggestion[]>(`/api/documents/${documentId}/suggestions`);
}

export async function updateSuggestionStatus(
  documentId: string,
  suggestionId: string,
  request: SuggestionUpdateRequest
): Promise<Suggestion> {
  return apiPut<Suggestion>(
    `/api/documents/${documentId}/suggestions/${suggestionId}`,
    request
  );
}
