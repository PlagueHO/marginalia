export type SuggestionStatus = "Pending" | "Accepted" | "Rejected" | "Modified";

export interface Suggestion {
  id: string;
  userId: string;
  documentId: string;
  paragraphId: string;
  rationale: string;
  proposedChange: string;
  status: SuggestionStatus;
  userSteeringInput?: string;
}
