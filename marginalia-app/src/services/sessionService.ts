import type { UserSession } from "@/types";
import { apiPost, apiGet } from "./api";

export async function createSession(): Promise<UserSession> {
  return apiPost<UserSession>("/api/sessions");
}

export async function getSession(sessionId: string): Promise<UserSession> {
  return apiGet<UserSession>(`/api/sessions/${sessionId}`);
}
