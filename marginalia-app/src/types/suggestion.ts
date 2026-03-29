import type { TextRange } from "./document";

export type SuggestionStatus = "Pending" | "Accepted" | "Rejected" | "Modified";

export interface Suggestion {
  id: string;
  userId: string;
  documentId: string;
  textRange: TextRange;
  rationale: string;
  proposedChange: string;
  status: SuggestionStatus;
  userSteeringInput?: string;
}
