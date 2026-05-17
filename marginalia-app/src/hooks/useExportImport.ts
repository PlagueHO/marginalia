import { useCallback, useEffect, useMemo, useState } from "react";
import type { ImportExportJob, JobStatus } from "@/types";
import * as exportImportService from "@/services/exportImportService";

interface UseExportImportState {
  exportJob: ImportExportJob | null;
  importJob: ImportExportJob | null;
  isStartingExport: boolean;
  isStartingImport: boolean;
  dataMessage: string | null;
  error: string | null;
}

const POLL_INTERVAL_MS = 2000;

function isActiveStatus(status?: JobStatus): boolean {
  return status === "Queued" || status === "Running";
}

function getExportFilename(date = new Date()): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `marginalia-export-${year}-${month}-${day}.zip`;
}

export function useExportImport() {
  const [state, setState] = useState<UseExportImportState>({
    exportJob: null,
    importJob: null,
    isStartingExport: false,
    isStartingImport: false,
    dataMessage: null,
    error: null,
  });

  const pollExportJob = useCallback(async (jobId: string) => {
    const job = await exportImportService.getExportJob(jobId);
    setState((prev) => {
      const nextState: UseExportImportState = { ...prev, exportJob: job };
      if (job.status === "Failed") {
        nextState.error = job.errorMessage ?? "Failed to export manuscripts";
      }

      return nextState;
    });
  }, []);

  const pollImportJob = useCallback(async (jobId: string) => {
    const job = await exportImportService.getImportJob(jobId);

    setState((prev) => {
      const nextState: UseExportImportState = { ...prev, importJob: job };
      if (job.status === "Completed") {
        const importedCount = job.counts?.documentsImported ?? 0;
        nextState.dataMessage = `Imported ${importedCount} manuscript${importedCount !== 1 ? "s" : ""} successfully.`;
      } else if (job.status === "Failed") {
        nextState.error = job.errorMessage ?? "Failed to import manuscripts";
      }

      return nextState;
    });
  }, []);

  useEffect(() => {
    if (!state.exportJob || !isActiveStatus(state.exportJob.status)) {
      return;
    }

    const intervalId = window.setInterval(() => {
      void pollExportJob(state.exportJob!.id).catch((err: unknown) => {
        const message = err instanceof Error ? err.message : "Failed to load export status";
        setState((prev) => ({ ...prev, error: message }));
      });
    }, POLL_INTERVAL_MS);

    return () => {
      window.clearInterval(intervalId);
    };
  }, [state.exportJob, pollExportJob]);

  useEffect(() => {
    if (!state.importJob || !isActiveStatus(state.importJob.status)) {
      return;
    }

    const intervalId = window.setInterval(() => {
      void pollImportJob(state.importJob!.id).catch((err: unknown) => {
        const message = err instanceof Error ? err.message : "Failed to load import status";
        setState((prev) => ({ ...prev, error: message }));
      });
    }, POLL_INTERVAL_MS);

    return () => {
      window.clearInterval(intervalId);
    };
  }, [state.importJob, pollImportJob]);

  const handleExport = useCallback(async () => {
    setState((prev) => ({
      ...prev,
      isStartingExport: true,
      exportJob: null,
      dataMessage: null,
      error: null,
    }));

    try {
      const jobId = await exportImportService.startExport();
      await pollExportJob(jobId);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to export manuscripts";
      setState((prev) => ({ ...prev, error: message }));
    } finally {
      setState((prev) => ({ ...prev, isStartingExport: false }));
    }
  }, [pollExportJob]);

  const handleDownload = useCallback(async () => {
    if (!state.exportJob || state.exportJob.status !== "Completed") {
      return;
    }

    const blob = await exportImportService.downloadExport(state.exportJob.id);
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = getExportFilename();
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
  }, [state.exportJob]);

  const handleImport = useCallback(async (file: File, overwrite = false) => {
    setState((prev) => ({
      ...prev,
      isStartingImport: true,
      importJob: null,
      dataMessage: null,
      error: null,
    }));

    try {
      const jobId = await exportImportService.startImport(file, overwrite);
      await pollImportJob(jobId);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to import manuscripts";
      setState((prev) => ({ ...prev, error: message }));
    } finally {
      setState((prev) => ({ ...prev, isStartingImport: false }));
    }
  }, [pollImportJob]);

  const clearStatus = useCallback(() => {
    setState((prev) => ({ ...prev, dataMessage: null, error: null }));
  }, []);

  const isExportRunning = useMemo(
    () => isActiveStatus(state.exportJob?.status),
    [state.exportJob]
  );

  const isImportRunning = useMemo(
    () => isActiveStatus(state.importJob?.status),
    [state.importJob]
  );

  return {
    exportJob: state.exportJob,
    importJob: state.importJob,
    isStartingExport: state.isStartingExport,
    isStartingImport: state.isStartingImport,
    isExportRunning,
    isImportRunning,
    dataMessage: state.dataMessage,
    error: state.error,
    handleExport,
    handleDownload,
    handleImport,
    clearStatus,
  };
}
