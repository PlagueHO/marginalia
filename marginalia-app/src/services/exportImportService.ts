import { apiGet, apiGetBlob, apiPost, apiPostFile } from "./api";
import type { ImportExportJob } from "@/types";

interface StartJobResponse {
  jobId: string;
}

export async function startExport(): Promise<string> {
  const response = await apiPost<StartJobResponse>("/api/exports");
  return response.jobId;
}

export async function getExportJob(jobId: string): Promise<ImportExportJob> {
  return apiGet<ImportExportJob>(`/api/exports/${jobId}`);
}

export async function downloadExport(jobId: string): Promise<Blob> {
  return apiGetBlob(`/api/exports/${jobId}/download`);
}

export async function startImport(file: File, overwrite = false): Promise<string> {
  const response = await apiPostFile<StartJobResponse>(
    `/api/imports?overwrite=${encodeURIComponent(String(overwrite))}`,
    file
  );

  return response.jobId;
}

export async function getImportJob(jobId: string): Promise<ImportExportJob> {
  return apiGet<ImportExportJob>(`/api/imports/${jobId}`);
}
