import type { Suggestion } from "./suggestion";

export type DocumentSource = "Local" | "GoogleDocs";

export type DocumentStatus = "Draft" | "Analyzed";

export interface Paragraph {
  id: string;
  text: string;
}

export interface Document {
  id: string;
  userId: string;
  filename: string;
  source: DocumentSource;
  title: string;
  status: DocumentStatus;
  createdAt: string;
  updatedAt: string;
  paragraphs: Paragraph[];
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
  paragraphCount: number;
}
