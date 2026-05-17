import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import {
  startExport,
  getExportJob,
  downloadExport,
  startImport,
  getImportJob,
} from "@/services/exportImportService";
import { setApiBaseUrl, setAccessCode } from "@/services/api";

const mockFetch = vi.fn();

beforeEach(() => {
  mockFetch.mockReset();
  vi.stubGlobal("fetch", mockFetch);
  setApiBaseUrl("http://localhost:5279");
  setAccessCode(null);
});

afterEach(() => {
  vi.restoreAllMocks();
});

describe("exportImportService", () => {
  it("startExport posts to /api/exports and returns jobId", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 202,
      json: () => Promise.resolve({ jobId: "job-1" }),
    });

    const result = await startExport();

    expect(result).toBe("job-1");
    expect(mockFetch).toHaveBeenCalledWith(
      "http://localhost:5279/api/exports",
      expect.objectContaining({ method: "POST" })
    );
  });

  it("getExportJob gets /api/exports/{jobId}", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ id: "job-1", status: "Queued" }),
    });

    const result = await getExportJob("job-1");

    expect(result.id).toBe("job-1");
    expect(mockFetch).toHaveBeenCalledWith(
      "http://localhost:5279/api/exports/job-1",
      expect.objectContaining({ method: "GET" })
    );
  });

  it("downloadExport gets blob from /api/exports/{jobId}/download", async () => {
    const blob = new Blob(["zip"], { type: "application/zip" });
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      blob: () => Promise.resolve(blob),
    });

    const result = await downloadExport("job-1");

    expect(result).toBe(blob);
    expect(mockFetch).toHaveBeenCalledWith(
      "http://localhost:5279/api/exports/job-1/download",
      expect.objectContaining({ method: "GET" })
    );
  });

  it("startImport posts form-data to /api/imports?overwrite=true", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 202,
      json: () => Promise.resolve({ jobId: "job-2" }),
    });

    const file = new File(["zip"], "backup.zip", { type: "application/zip" });
    const result = await startImport(file, true);

    expect(result).toBe("job-2");
    const [url, options] = mockFetch.mock.calls[0] as [string, RequestInit];
    expect(url).toBe("http://localhost:5279/api/imports?overwrite=true");
    expect(options.method).toBe("POST");
    expect(options.body).toBeInstanceOf(FormData);
  });

  it("getImportJob gets /api/imports/{jobId}", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ id: "job-2", status: "Completed" }),
    });

    const result = await getImportJob("job-2");

    expect(result.id).toBe("job-2");
    expect(mockFetch).toHaveBeenCalledWith(
      "http://localhost:5279/api/imports/job-2",
      expect.objectContaining({ method: "GET" })
    );
  });
});
