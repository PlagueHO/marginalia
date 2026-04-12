import type { Document, DocumentSummary } from "./document";
import type { Suggestion, SuggestionStatus } from "./suggestion";
import type { UserSession } from "./session";

export interface UploadResponse {
  document: Document;
  sessionId: string;
}

export interface PasteRequest {
  content: string;
  filename?: string;
  title?: string;
}

export interface DocumentListResponse {
  documents: DocumentSummary[];
}

export interface AnalyzeRequest {
  documentId: string;
  userInstructions?: string;
  toneGuidance?: string;

  // Backward-compatible fields accepted by the API.
  userGuidance?: string;
  tone?: string;
}

export interface SuggestionUpdateRequest {
  status: SuggestionStatus;
  userSteeringInput?: string;
}

export interface LlmConfig {
  endpoint?: string;
  modelName?: string;
  authMethod?: string;
  isConfigured?: boolean;
}

export interface LlmHealthResult {
  healthy: boolean;
  message: string;
}

export interface ExportRequest {
  documentId: string;
  format: "docx";
}

export interface ApiError {
  message: string;
  statusCode: number;
}

export interface AccessControlStatus {
  accessCodeRequired: boolean;
}

export type {
  Document,
  Suggestion,
  SuggestionStatus,
  UserSession,
};
