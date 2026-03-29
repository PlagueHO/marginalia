import type { Document } from "./document";
import type { Suggestion, SuggestionStatus } from "./suggestion";
import type { UserSession } from "./session";

export interface UploadResponse {
  document: Document;
  sessionId: string;
}

export interface PasteRequest {
  content: string;
  filename?: string;
}

export interface AnalyzeRequest {
  documentId: string;
  content: string;
  userGuidance?: string;
  tone?: string;
  selectedRange?: {
    start: number;
    end: number;
  };
}

export interface SuggestionUpdateRequest {
  status: SuggestionStatus;
  modifiedText?: string;
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

export type {
  Document,
  Suggestion,
  SuggestionStatus,
  UserSession,
};
