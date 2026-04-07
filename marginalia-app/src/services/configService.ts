import type { AccessControlStatus, LlmConfig, LlmHealthResult } from "@/types";
import { apiGet } from "./api";

export async function getAccessStatus(): Promise<AccessControlStatus> {
  return apiGet<AccessControlStatus>("/api/config/access-status");
}

export async function getLlmConfig(): Promise<LlmConfig> {
  return apiGet<LlmConfig>("/api/config/llm");
}

export async function checkHealth(): Promise<LlmHealthResult> {
  return apiGet<LlmHealthResult>("/api/config/llm/health");
}
