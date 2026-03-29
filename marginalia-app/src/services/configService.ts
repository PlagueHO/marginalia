import type { LlmConfig, LlmHealthResult } from "@/types";
import { apiGet } from "./api";

export async function getLlmConfig(): Promise<LlmConfig> {
  return apiGet<LlmConfig>("/api/config/llm");
}

export async function checkHealth(): Promise<LlmHealthResult> {
  return apiGet<LlmHealthResult>("/api/config/llm/health");
}
