import type { AccessControlStatus, LlmHealthResult } from "@/types";
import { apiGet } from "./api";

export async function getAccessStatus(): Promise<AccessControlStatus> {
  return apiGet<AccessControlStatus>("/api/config/access-status");
}

export async function checkHealth(): Promise<LlmHealthResult> {
  return apiGet<LlmHealthResult>("/api/config/llm/health");
}
