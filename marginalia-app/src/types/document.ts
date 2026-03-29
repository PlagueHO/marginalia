import type { Suggestion } from "./suggestion";

export type DocumentSource = "Local" | "GoogleDocs";

export type DocumentStatus = "Draft" | "Analyzed";

export interface TextRange {
  start: number;
  end: number;
}

export interface Document {
  id: string;
  userId: string;
  filename: string;
  source: DocumentSource;
  content: string;
  title: string;
  status: DocumentStatus;
  createdAt: string;
  updatedAt: string;
  suggestions: Suggestion[];
}

export interface DocumentSummary {
  id: string;
  title: string;
  filename: string;
  source: DocumentSource;
  status: DocumentStatus;
  createdAt: string;
  updatedAt: string;
  suggestionCount: number;
}
