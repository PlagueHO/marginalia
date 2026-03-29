import type { Suggestion } from "./suggestion";

export type DocumentSource = "Local" | "GoogleDocs";

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
  suggestions: Suggestion[];
}
