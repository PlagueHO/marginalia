import { useState, useRef } from "react";
import { AppHeader } from "@/components/AppHeader";
import { useLlmConfig } from "@/hooks/useLlmConfig";
import { useExportImport } from "@/hooks/useExportImport";
import { usePageTitle } from "@/hooks/usePageTitle";
import { useTheme } from "@/hooks/useTheme";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Separator } from "@/components/ui/separator";
import { Spinner } from "@/components/ui/spinner";
import { cn, headingText } from "@/lib/utils";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { CheckCircle2, Download, Moon, Monitor, RefreshCw, Sun, Upload, XCircle } from "lucide-react";

export function SettingsPage() {
  usePageTitle("Settings");

  const llmConfig = useLlmConfig();
  const { theme, setTheme } = useTheme();
  const [importFile, setImportFile] = useState<File | null>(null);
  const [overwrite, setOverwrite] = useState(false);
  const [fileError, setFileError] = useState<string | null>(null);
  const [downloadError, setDownloadError] = useState<string | null>(null);
  const importInputRef = useRef<HTMLInputElement>(null);
  const {
    exportJob,
    importJob,
    isStartingExport,
    isStartingImport,
    isExportRunning,
    isImportRunning,
    dataMessage,
    error,
    handleExport,
    handleDownload,
    handleImport,
    clearStatus,
  } = useExportImport();

  return (
    <div className="flex flex-col h-screen">
      <AppHeader />

      <main id="main-content" tabIndex={-1} className="flex-1 overflow-auto">
        <div className="mx-auto w-full max-w-4xl space-y-8 px-4 py-8 sm:px-6 sm:py-10">
          <div>
            <h1 className={cn(headingText, "text-3xl")}>Settings</h1>
            <p className="text-sm text-muted-foreground">
              Appearance, backend status, and manuscript backup controls.
            </p>
          </div>

          <Separator />

          <section className="space-y-4">
            <h2 className={cn(headingText, "text-lg")}>Appearance</h2>
            <div className="space-y-2">
              <Label htmlFor="theme-select">Theme</Label>
              <Select value={theme} onValueChange={(v) => setTheme(v as "system" | "light" | "dark")}>
                <SelectTrigger id="theme-select" aria-describedby="theme-help" className="w-full sm:max-w-64">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {([
                    { value: "light", label: "Light", icon: Sun },
                    { value: "dark", label: "Dark", icon: Moon },
                    { value: "system", label: "System", icon: Monitor },
                  ] as const).map(({ value: v, label, icon: Icon }) => (
                    <SelectItem key={v} value={v}>
                      <div className="flex items-center gap-2">
                        <Icon className="size-4" aria-hidden="true" />
                        {label}
                      </div>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <p id="theme-help" className="text-sm text-muted-foreground">
                Select &quot;System&quot; to automatically match your browser or OS preference.
              </p>
            </div>
          </section>

          <Separator />

          <section className="space-y-4">
            <h2 className={cn(headingText, "text-lg")}>Data</h2>
            <p className="text-sm text-muted-foreground">
              Export all manuscripts and settings to a ZIP backup, or restore from an existing backup.
            </p>

            <div className="grid gap-4 md:grid-cols-2">
              <Card>
                <CardHeader>
                  <CardTitle className="text-base"><h3>Export</h3></CardTitle>
                </CardHeader>
                <CardContent className="flex h-full flex-col space-y-3">
                  <p className="text-sm text-muted-foreground">
                    Download a backup ZIP of your manuscripts and their suggestions.
                  </p>
                  <p className="text-sm">Includes manuscripts and suggestions. Revision history is not included.</p>
                  <div className="mt-auto flex flex-col gap-2 sm:flex-row sm:items-center">
                    <Button
                      onClick={() => void handleExport()}
                      disabled={isStartingExport || isStartingImport || isExportRunning || isImportRunning}
                      className="gap-2"
                    >
                      {(isStartingExport || isExportRunning) ? <Spinner /> : <Download className="size-4" aria-hidden="true" />}
                      {(isStartingExport || isExportRunning) ? "Exporting..." : "Start Export"}
                    </Button>
                    <Button
                      variant="outline"
                      onClick={async () => {
                        setDownloadError(null);
                        try {
                          await handleDownload();
                        } catch (err) {
                          setDownloadError(err instanceof Error ? err.message : "Download failed. Please try again.");
                        }
                      }}
                      disabled={exportJob?.status !== "Completed" || isStartingExport || isExportRunning}
                      aria-describedby="export-download-help"
                    >
                      Download ZIP
                    </Button>
                  </div>
                  <p id="export-download-help" className="text-sm text-muted-foreground">Download ZIP becomes available when the export completes.</p>
                  {exportJob && (
                    <p role="status" className="text-sm text-muted-foreground" data-testid="export-job-status">
                      {exportJob.status} ({exportJob.progressPercentage}%)
                      {exportJob.currentStage ? ` - ${exportJob.currentStage}` : ""}
                    </p>
                  )}
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle className="text-base"><h3>Import</h3></CardTitle>
                </CardHeader>
                <CardContent className="flex h-full flex-col space-y-3">
                  <p className="text-sm text-muted-foreground">
                    Restore manuscripts from a previously exported ZIP backup.
                  </p>
                  <div className="space-y-2">
                    <Label htmlFor="import-file">Backup ZIP <span aria-hidden="true">*</span></Label>
                    <Input
                      ref={importInputRef}
                      id="import-file"
                      type="file"
                      accept=".zip"
                      onChange={(event) => {
                        clearStatus();
                        setFileError(null);
                        setImportFile(event.target.files?.[0] ?? null);
                      }}
                      aria-invalid={fileError ? true : undefined}
                      aria-required="true"
                      aria-describedby={fileError ? "import-file-error" : undefined}
                      disabled={isStartingImport || isStartingExport || isImportRunning || isExportRunning}
                    />
                    {fileError && <p id="import-file-error" role="alert" className="text-sm text-destructive">{fileError}</p>}
                  </div>
                  <label className="flex items-center gap-2 text-sm font-medium">
                    <input
                      type="checkbox"
                      checked={overwrite}
                      onChange={(e) => {
                        setOverwrite(e.target.checked);
                      }}
                      className="h-4 w-4 accent-primary"
                    />
                    Overwrite existing records
                  </label>
                  <div className="mt-auto flex flex-col gap-2 sm:flex-row sm:items-center">
                    <Button
                      onClick={() => {
                        if (!importFile || !importFile.name.toLowerCase().endsWith(".zip")) {
                          setFileError("Choose a ZIP backup before starting the import.");
                          importInputRef.current?.focus();
                          return;
                        }
                        setFileError(null);
                        void handleImport(importFile, overwrite);
                      }}
                      disabled={isStartingImport || isStartingExport || isImportRunning || isExportRunning}
                      className="gap-2"
                    >
                      {(isStartingImport || isImportRunning) ? <Spinner /> : <Upload className="size-4" aria-hidden="true" />}
                      {(isStartingImport || isImportRunning) ? "Importing..." : "Start Import"}
                    </Button>
                  </div>
                  {importJob && (
                    <p role={dataMessage ? undefined : "status"} className="text-sm text-muted-foreground" data-testid="import-job-status">
                      {importJob.status} ({importJob.progressPercentage}%)
                      {importJob.currentStage ? ` - ${importJob.currentStage}` : ""}
                    </p>
                  )}
                </CardContent>
              </Card>
            </div>

            {dataMessage && (
              <div role="status" className="rounded-md bg-status-accepted/10 px-3 py-2 text-sm text-status-accepted">
                {dataMessage}
              </div>
            )}

            {(error || downloadError) && (
              <div role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                {error || downloadError}
              </div>
            )}
          </section>

          <Separator />

          <Card>
            <CardHeader>
              <CardTitle className={cn(headingText, "text-lg")}><h2>Backend Status</h2></CardTitle>
              <CardDescription>Monitor connections to the AI backend and authentication service.</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                <div aria-live="polite" className="flex-1 space-y-3">
                  <div className="space-y-1">
                    {llmConfig.isCheckingHealth ? (
                      <div className="flex items-center gap-2 text-sm">
                        <Spinner />
                        <span className="text-muted-foreground">Checking AI model…</span>
                      </div>
                    ) : llmConfig.healthResult?.healthy ? (
                      <div className="flex items-center gap-2 text-sm">
                        <CheckCircle2 className="size-4 text-green-600 dark:text-green-400" aria-hidden="true" />
                        <span className="font-medium text-green-700 dark:text-green-300">AI Model connected</span>
                      </div>
                    ) : llmConfig.healthResult ? (
                      <div className="space-y-1">
                        <div className="flex items-center gap-2 text-sm">
                          <XCircle className="size-4 text-red-600 dark:text-red-400" aria-hidden="true" />
                          <span className="font-medium text-red-700 dark:text-red-300">AI Model disconnected</span>
                        </div>
                        <p className="text-xs text-muted-foreground ml-6">{llmConfig.healthResult.message}</p>
                      </div>
                    ) : (
                      <p className="text-sm text-muted-foreground">AI model — not yet checked</p>
                    )}
                  </div>

                  <div className="space-y-1">
                    {llmConfig.isCheckingAccess ? (
                      <div className="flex items-center gap-2 text-sm">
                        <Spinner />
                        <span className="text-muted-foreground">Checking authentication…</span>
                      </div>
                    ) : llmConfig.accessStatus !== null ? (
                      <div className="flex items-center gap-2 text-sm">
                        <CheckCircle2 className="size-4 text-green-600 dark:text-green-400" aria-hidden="true" />
                        <span className="font-medium text-green-700 dark:text-green-300">
                          {llmConfig.accessStatus.accessCodeRequired
                            ? "Access code required"
                            : "Entra ID (Default Azure Credential)"}
                        </span>
                      </div>
                    ) : (
                      <p className="text-sm text-muted-foreground">Authentication — not yet checked</p>
                    )}
                  </div>
                </div>

                <Button
                  variant="outline"
                  onClick={() => void llmConfig.checkHealth()}
                  disabled={llmConfig.isCheckingHealth || llmConfig.isCheckingAccess}
                  className="w-full sm:w-auto"
                >
                  <RefreshCw className="size-4" aria-hidden="true" />
                  <span>Check Connection</span>
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>
      </main>
    </div>
  );
}
