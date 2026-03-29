import type { ApiError } from "@/types";

declare const __API_BASE_URL__: string;

const DEFAULT_BASE_URL = typeof __API_BASE_URL__ !== 'undefined' && __API_BASE_URL__ !== ''
  ? __API_BASE_URL__
  : "http://localhost:5279";

const DEFAULT_USER_ID = "_anonymous";

let baseUrl = DEFAULT_BASE_URL;
let currentUserId: string = DEFAULT_USER_ID;

export function setApiBaseUrl(url: string): void {
  baseUrl = url.replace(/\/+$/, "");
}

export function getApiBaseUrl(): string {
  return baseUrl;
}

export function setUserId(userId: string): void {
  currentUserId = userId || DEFAULT_USER_ID;
}

export function getUserId(): string {
  return currentUserId;
}

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const error: ApiError = {
      message: response.statusText || "Request failed",
      statusCode: response.status,
    };

    try {
      const body = await response.json() as { message?: string };
      if (body.message) {
        error.message = body.message;
      }
    } catch {
      // Use default message
    }

    throw error;
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export async function apiGet<T>(path: string): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
      "X-User-Id": currentUserId,
    },
  });
  return handleResponse<T>(response);
}

export async function apiPost<T>(path: string, body?: unknown): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "X-User-Id": currentUserId,
    },
    body: body ? JSON.stringify(body) : undefined,
  });
  return handleResponse<T>(response);
}

export async function apiPut<T>(path: string, body: unknown): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
      "X-User-Id": currentUserId,
    },
    body: JSON.stringify(body),
  });
  return handleResponse<T>(response);
}

export async function apiPostFile<T>(
  path: string,
  file: File
): Promise<T> {
  const formData = new FormData();
  formData.append("file", file);

  const response = await fetch(`${baseUrl}${path}`, {
    method: "POST",
    headers: {
      "X-User-Id": currentUserId,
    },
    body: formData,
  });
  return handleResponse<T>(response);
}

export async function apiGetBlob(path: string): Promise<Blob> {
  const response = await fetch(`${baseUrl}${path}`, {
    method: "GET",
    headers: {
      "X-User-Id": currentUserId,
    },
  });

  if (!response.ok) {
    throw {
      message: response.statusText || "Download failed",
      statusCode: response.status,
    } satisfies ApiError;
  }

  return response.blob();
}
