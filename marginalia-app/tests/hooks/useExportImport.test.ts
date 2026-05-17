import { act, renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useExportImport } from "@/hooks/useExportImport";

vi.mock("@/services/exportImportService", () => ({
  startExport: vi.fn(),
  getExportJob: vi.fn(),
  downloadExport: vi.fn(),
  startImport: vi.fn(),
  getImportJob: vi.fn(),
}));

import * as exportImportService from "@/services/exportImportService";

const mockStartExport = vi.mocked(exportImportService.startExport);
const mockGetExportJob = vi.mocked(exportImportService.getExportJob);
const mockDownloadExport = vi.mocked(exportImportService.downloadExport);
const mockStartImport = vi.mocked(exportImportService.startImport);
const mockGetImportJob = vi.mocked(exportImportService.getImportJob);

describe("useExportImport", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("starts export job and stores initial export job state", async () => {
    mockStartExport.mockResolvedValueOnce("job-1");
    mockGetExportJob.mockResolvedValueOnce({
      id: "job-1",
      jobType: "Export",
      status: "Queued",
      createdAt: "2026-05-17T12:00:00Z",
      progressPercentage: 0,
      totalItems: 0,
      processedItems: 0,
      overwriteExisting: false,
    });

    const { result } = renderHook(() => useExportImport());

    await act(async () => {
      await result.current.handleExport();
    });

    expect(mockStartExport).toHaveBeenCalledTimes(1);
    expect(result.current.exportJob?.id).toBe("job-1");
    expect(result.current.isStartingExport).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it("polls export job while active and updates to completed", async () => {
    vi.useFakeTimers();
    try {
      mockStartExport.mockResolvedValueOnce("job-1");
      mockGetExportJob
        .mockResolvedValueOnce({
          id: "job-1",
          jobType: "Export",
          status: "Queued",
          createdAt: "2026-05-17T12:00:00Z",
          progressPercentage: 0,
          totalItems: 0,
          processedItems: 0,
          overwriteExisting: false,
        })
        .mockResolvedValueOnce({
          id: "job-1",
          jobType: "Export",
          status: "Completed",
          createdAt: "2026-05-17T12:00:00Z",
          progressPercentage: 100,
          totalItems: 2,
          processedItems: 2,
          overwriteExisting: false,
        });

      const { result } = renderHook(() => useExportImport());

      await act(async () => {
        await result.current.handleExport();
      });

      await act(async () => {
        await vi.advanceTimersByTimeAsync(2000);
      });

      expect(mockGetExportJob).toHaveBeenCalledTimes(2);
      expect(result.current.exportJob?.status).toBe("Completed");
    } finally {
      vi.useRealTimers();
    }
  });

  it("downloads zip when export job is completed", async () => {
    vi.useFakeTimers();
    try {
      vi.setSystemTime(new Date("2026-05-17T12:00:00Z"));

      mockStartExport.mockResolvedValueOnce("job-1");
      mockGetExportJob.mockResolvedValueOnce({
        id: "job-1",
        jobType: "Export",
        status: "Completed",
        createdAt: "2026-05-17T12:00:00Z",
        progressPercentage: 100,
        totalItems: 1,
        processedItems: 1,
        overwriteExisting: false,
      });

      const blob = new Blob(["zip"], { type: "application/zip" });
      mockDownloadExport.mockResolvedValueOnce(blob);

      const createObjectUrl = vi.spyOn(URL, "createObjectURL").mockReturnValue("blob:test");
      const revokeObjectUrl = vi.spyOn(URL, "revokeObjectURL").mockImplementation(() => undefined);

      const originalCreateElement = document.createElement.bind(document);
      const anchor = originalCreateElement("a");
      const clickSpy = vi.spyOn(anchor, "click").mockImplementation(() => undefined);
      vi.spyOn(document, "createElement").mockImplementation((tagName: string) => {
        if (tagName === "a") {
          return anchor;
        }
        return originalCreateElement(tagName);
      });

      const appendSpy = vi.spyOn(document.body, "appendChild");

      const { result } = renderHook(() => useExportImport());

      await act(async () => {
        await result.current.handleExport();
      });

      await act(async () => {
        await result.current.handleDownload();
      });

      expect(mockDownloadExport).toHaveBeenCalledWith("job-1");
      expect(createObjectUrl).toHaveBeenCalledWith(blob);
      expect(anchor.download).toMatch(/^marginalia-export-2026-05-(17|18)\.zip$/);
      expect(clickSpy).toHaveBeenCalledTimes(1);
      expect(appendSpy).toHaveBeenCalledWith(anchor);
      expect(revokeObjectUrl).toHaveBeenCalledWith("blob:test");
    } finally {
      vi.useRealTimers();
    }
  });

  it("captures export start failures", async () => {
    mockStartExport.mockRejectedValueOnce(new Error("export failed"));

    const { result } = renderHook(() => useExportImport());

    await act(async () => {
      await result.current.handleExport();
    });

    expect(result.current.error).toBe("export failed");
    expect(result.current.isStartingExport).toBe(false);
  });

  it("starts import with overwrite and stores completion message", async () => {
    const file = new File(["content"], "backup.zip", { type: "application/zip" });
    mockStartImport.mockResolvedValueOnce("job-2");
    mockGetImportJob.mockResolvedValueOnce({
      id: "job-2",
      jobType: "Import",
      status: "Completed",
      createdAt: "2026-05-17T12:00:00Z",
      progressPercentage: 100,
      totalItems: 3,
      processedItems: 3,
      overwriteExisting: true,
      counts: {
        documentsImported: 3,
        documentsSkipped: 0,
        failed: 0,
      },
    });

    const { result } = renderHook(() => useExportImport());

    await act(async () => {
      await result.current.handleImport(file, true);
    });

    expect(mockStartImport).toHaveBeenCalledWith(file, true);
    expect(result.current.importJob?.id).toBe("job-2");
    expect(result.current.dataMessage).toBe("Imported 3 manuscripts successfully.");
    expect(result.current.error).toBeNull();
    expect(result.current.isStartingImport).toBe(false);
  });

  it("clears status values", async () => {
    mockStartExport.mockRejectedValueOnce(new Error("export failed"));

    const { result } = renderHook(() => useExportImport());

    await act(async () => {
      await result.current.handleExport();
    });

    act(() => {
      result.current.clearStatus();
    });

    expect(result.current.dataMessage).toBeNull();
    expect(result.current.error).toBeNull();
  });
});
