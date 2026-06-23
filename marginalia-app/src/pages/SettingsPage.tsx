import { useState } from "react";
import { AppHeader } from "@/components/AppHeader";
import { useLlmConfig } from "@/hooks/useLlmConfig";
import { useExportImport } from "@/hooks/useExportImport";
import { usePageTitle } from "@/hooks/usePageTitle";
import { useTheme } from "@/hooks/useTheme";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { Spinner } from "@/components/ui/spinner";
import { cn, gradientText } from "@/lib/utils";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { CheckCircle2, Download, Moon, Monitor, RefreshCw, Sun, Upload, XCircle } from "lucide-react";

export function SettingsPage() {
  usePageTitle("Settings");

  const llmConfig = useLlmConfig();
  const { theme, setTheme } = useTheme();
  const [importFile, setImportFile] = useState<File | null>(null);
  const [overwrite, setOverwrite] = useState(false);
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

      <main id="main-content" className="flex-1 overflow-auto">
        <div className="mx-auto w-full max-w-4xl space-y-6 px-4 py-6 sm:py-8">
          <div>
            <h1 className={cn(gradientText, "text-2xl")}>Settings</h1>
            <p className="text-sm text-muted-foreground">
              Appearance, backend status, and manuscript backup controls.
            </p>
          </div>

          <Separator />

          <section className="space-y-4">
            <h2 className={cn(gradientText, "text-lg")}>Appearance</h2>
            <div className="space-y-2">
              <Label htmlFor="theme-select">Theme</Label>
              <Select value={theme} onValueChange={(v) => setTheme(v as "system" | "light" | "dark")}>
                <SelectTrigger id="theme-select" className="w-full sm:max-w-64">
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
              <p className="text-sm text-muted-foreground">
                Select &quot;System&quot; to automatically match your browser or OS preference.
              </p>
            </div>
          </section>

          <Separator />

          <section className="space-y-4">
            <h2 className={cn(gradientText, "text-lg")}>Data</h2>
            <p className="text-sm text-muted-foreground">
              Export all manuscripts and settings to a ZIP backup, or restore from an existing backup.
            </p>

            <div className="grid gap-4 md:grid-cols-2">
              <Card>
                <CardHeader>
                  <CardTitle className="text-base">Export</CardTitle>
                </CardHeader>
                <CardContent className="flex h-full flex-col space-y-3">
                  <p className="text-sm text-muted-foreground">
                    Download a backup ZIP of your manuscripts and their suggestions.
                  </p>
                  <div className="space-y-2">
                    <label className="flex items-center gap-2 text-sm font-medium">
                      <input type="checkbox" defaultChecked readOnly className="h-4 w-4 accent-primary" />
                      Manuscripts
                    </label>
                    <label className="flex items-center gap-2 text-sm font-medium">
                      <input type="checkbox" defaultChecked readOnly className="h-4 w-4 accent-primary" />
                      Suggestions
                    </label>
                    <label className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
                      <input type="checkbox" disabled className="h-4 w-4" />
                      Include revision history
                    </label>
                  </div>
                  <div className="mt-auto flex flex-col gap-2 sm:flex-row sm:items-center">
                    <Button
                      onClick={() => void handleExport()}
                      disabled={isStartingExport || isStartingImport || isExportRunning || isImportRunning}
                      className="min-h-11 gap-2"
                    >
                      {(isStartingExport || isExportRunning) ? <Spinner /> : <Download className="size-4" aria-hidden="true" />}
                      {(isStartingExport || isExportRunning) ? "Exporting..." : "Start Export"}
                    </Button>
                    <Button
                      variant="outline"
                      onClick={() => void handleDownload()}
                      disabled={exportJob?.status !== "Completed" || isStartingExport || isExportRunning}
                      className="min-h-11"
                    >
                      Download ZIP
                    </Button>
                  </div>
                  {exportJob && (
                    <p className="text-sm text-muted-foreground" data-testid="export-job-status">
                      {exportJob.status} ({exportJob.progressPercentage}%)
                      {exportJob.currentStage ? ` - ${exportJob.currentStage}` : ""}
                    </p>
                  )}
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle className="text-base">Import</CardTitle>
                </CardHeader>
                <CardContent className="flex h-full flex-col space-y-3">
                  <p className="text-sm text-muted-foreground">
                    Restore manuscripts from a previously exported ZIP backup.
                  </p>
                  <div className="space-y-2">
                    <Label htmlFor="import-file">Backup ZIP</Label>
                    <input
                      id="import-file"
                      type="file"
                      accept=".zip"
                      onChange={(event) => {
                        clearStatus();
                        setImportFile(event.target.files?.[0] ?? null);
                      }}
                      className="block w-full text-sm"
                    />
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
                        if (importFile) {
                          void handleImport(importFile, overwrite);
                        }
                      }}
                      disabled={!importFile || isStartingImport || isStartingExport || isImportRunning || isExportRunning}
                      className="min-h-11 gap-2"
                    >
                      {(isStartingImport || isImportRunning) ? <Spinner /> : <Upload className="size-4" aria-hidden="true" />}
                      {(isStartingImport || isImportRunning) ? "Importing..." : "Start Import"}
                    </Button>
                  </div>
                  {importJob && (
                    <p className="text-sm text-muted-foreground" data-testid="import-job-status">
                      {importJob.status} ({importJob.progressPercentage}%)
                      {importJob.currentStage ? ` - ${importJob.currentStage}` : ""}
                    </p>
                  )}
                </CardContent>
              </Card>
            </div>

            {dataMessage && (
              <div className="rounded-md bg-green-50 px-3 py-2 text-sm text-green-800 dark:bg-green-900/20 dark:text-green-200">
                {dataMessage}
              </div>
            )}

            {error && (
              <div className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-800 dark:bg-red-900/20 dark:text-red-200">
                {error}
              </div>
            )}
          </section>

          <Separator />

          <Card>
            <CardHeader>
              <CardTitle className={cn(gradientText, "text-lg")}>Backend Status</CardTitle>
              <CardDescription>Monitor connections to the AI backend and authentication service.</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                <div className="flex-1 space-y-3">
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
                  size="sm"
                  onClick={() => void llmConfig.checkHealth()}
                  disabled={llmConfig.isCheckingHealth || llmConfig.isCheckingAccess}
                  className="min-h-11 w-full sm:w-auto"
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
