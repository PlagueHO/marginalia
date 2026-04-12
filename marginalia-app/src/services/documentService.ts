import type {
  Document,
  UploadResponse,
  PasteRequest,
  DocumentListResponse,
  AnalyzeRequest,
  Suggestion,
} from "@/types";
import { apiGet, apiPost, apiPostFile, apiGetBlob, apiPut, apiDelete } from "./api";

export async function listDocuments(): Promise<DocumentListResponse> {
  return apiGet<DocumentListResponse>("/api/documents");
}

export async function uploadDocument(file: File, title?: string): Promise<UploadResponse> {
  return apiPostFile<UploadResponse>("/api/documents/upload", file, title ? { title } : undefined);
}

export async function pasteDocument(
  request: PasteRequest
): Promise<UploadResponse> {
  return apiPost<UploadResponse>("/api/documents/paste", request);
}

export async function getDocument(documentId: string): Promise<Document> {
  return apiGet<Document>(`/api/documents/${documentId}`);
}

export async function analyzeDocument(
  request: AnalyzeRequest
): Promise<Suggestion[]> {
  return apiPost<Suggestion[]>(`/api/documents/${request.documentId}/analyze`, request);
}

export async function analyzeParagraph(
  documentId: string,
  paragraphId: string,
  request?: Omit<AnalyzeRequest, "documentId">
): Promise<Suggestion[]> {
  return apiPost<Suggestion[]>(
    `/api/documents/${documentId}/paragraphs/${paragraphId}/analyze`,
    request ?? {}
  );
}

export async function exportDocument(
  documentId: string
): Promise<Blob> {
  return apiGetBlob(`/api/documents/${documentId}/export`);
}

export async function renameDocument(
  documentId: string,
  title: string
): Promise<Document> {
  return apiPut<Document>(`/api/documents/${documentId}/title`, { title });
}

export async function deleteDocument(documentId: string): Promise<void> {
  return apiDelete<void>(`/api/documents/${documentId}`);
}
