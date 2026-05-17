import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi, beforeEach } from "vitest";
import { MemoryRouter } from "react-router-dom";
import { TooltipProvider } from "@/components/ui/tooltip";
import { SettingsPage } from "@/pages/SettingsPage";

vi.mock("@/hooks/usePageTitle", () => ({
  usePageTitle: vi.fn(),
}));

vi.mock("@/components/AppHeader", () => ({
  AppHeader: () => <div>Header</div>,
}));

const mockSetTheme = vi.fn();

vi.mock("@/hooks/useTheme", () => ({
  useTheme: () => ({
    theme: "system",
    resolvedTheme: "light",
    setTheme: mockSetTheme,
    toggleTheme: vi.fn(),
  }),
}));

const mockCheckHealth = vi.fn(async () => undefined);
const mockHandleExport = vi.fn(async () => undefined);
const mockHandleDownload = vi.fn();
const mockHandleImport = vi.fn(async () => undefined);
const mockClearStatus = vi.fn();

const mockUseExportImportState = {
  exportJob: null,
  importJob: null,
  isStartingExport: false,
  isStartingImport: false,
  isExportRunning: false,
  isImportRunning: false,
  dataMessage: null,
  error: null,
  handleExport: mockHandleExport,
  handleDownload: mockHandleDownload,
  handleImport: mockHandleImport,
  clearStatus: mockClearStatus,
};

vi.mock("@/hooks/useLlmConfig", () => ({
  useLlmConfig: () => ({
    isCheckingHealth: false,
    healthResult: { healthy: true, message: "Connected" },
    isCheckingAccess: false,
    accessStatus: { accessCodeRequired: false },
    checkHealth: mockCheckHealth,
  }),
}));

vi.mock("@/hooks/useExportImport", () => ({
  useExportImport: () => mockUseExportImportState,
}));

describe("SettingsPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseExportImportState.exportJob = null;
    mockUseExportImportState.importJob = null;
    mockUseExportImportState.isStartingExport = false;
    mockUseExportImportState.isStartingImport = false;
    mockUseExportImportState.isExportRunning = false;
    mockUseExportImportState.isImportRunning = false;
    mockUseExportImportState.dataMessage = null;
    mockUseExportImportState.error = null;
  });

  function renderPage() {
    return render(
      <MemoryRouter>
        <TooltipProvider>
          <SettingsPage />
        </TooltipProvider>
      </MemoryRouter>
    );
  }

  it("renders settings, appearance, data, and backend status sections", () => {
    renderPage();

    expect(screen.getByRole("heading", { name: "Settings" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Appearance" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Data" })).toBeInTheDocument();
    expect(screen.getByText("Backend Status")).toBeInTheDocument();
  });

  it("starts export when Start Export is clicked", async () => {
    const user = userEvent.setup();
    renderPage();

    await user.click(screen.getByRole("button", { name: /start export/i }));

    expect(mockHandleExport).toHaveBeenCalledTimes(1);
  });

  it("starts import when file selected and Start Import clicked", async () => {
    const user = userEvent.setup();
    renderPage();

    const file = new File(["zip"], "backup.zip", { type: "application/zip" });
    const input = screen.getByLabelText(/backup zip/i);

    await user.upload(input, file);
    await user.click(screen.getByRole("button", { name: /start import/i }));

    expect(mockHandleImport).toHaveBeenCalledWith(file, false);
  });

  it("disables Download ZIP until export job is completed", () => {
    mockUseExportImportState.exportJob = {
      id: "job-1",
      jobType: "Export",
      status: "Running",
      createdAt: "2026-05-17T12:00:00Z",
      progressPercentage: 50,
      totalItems: 2,
      processedItems: 1,
      overwriteExisting: false,
    };

    renderPage();

    expect(screen.getByRole("button", { name: /download zip/i })).toBeDisabled();
  });

  it("checks backend connection when Check Connection is clicked", async () => {
    const user = userEvent.setup();
    renderPage();

    await user.click(screen.getByRole("button", { name: /check connection/i }));

    expect(mockCheckHealth).toHaveBeenCalledTimes(1);
  });
});
