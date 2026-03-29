import { useState, useCallback } from "react";
import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Settings, PlusCircle, BookOpen } from "lucide-react";
import { Separator } from "@/components/ui/separator";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { LlmConfigDialog } from "./LlmConfigDialog";
import { ExportControls } from "./ExportControls";
import type { LlmConfig, LlmHealthResult } from "@/types";

interface AppHeaderProps {
  documentId?: string;
  filename?: string;
  llmConfig: LlmConfig;
  isConfigLoading: boolean;
  isCheckingHealth: boolean;
  healthResult: LlmHealthResult | null;
  onNewSession: () => void;
  onCheckHealth: () => Promise<void>;
}

export function AppHeader({
  documentId,
  filename,
  llmConfig,
  isConfigLoading,
  isCheckingHealth,
  healthResult,
  onNewSession,
  onCheckHealth,
}: AppHeaderProps) {
  const [isConfigOpen, setIsConfigOpen] = useState(false);

  const handleNewSession = useCallback(() => {
    onNewSession();
  }, [onNewSession]);

  return (
    <header className="flex items-center justify-between px-4 py-2.5 border-b border-border/50 bg-linear-to-r from-background via-background to-muted/30 dark:from-zinc-950 dark:via-zinc-900/80 dark:to-zinc-800/40 backdrop-blur-md supports-backdrop-filter:bg-background/60 sticky top-0 z-50 shadow-sm">
      <div className="flex items-center gap-3">
        <Link to="/" className="flex items-center gap-3 hover:opacity-80 transition-opacity">
          <BookOpen className="h-5 w-5 text-violet-400" aria-hidden="true" />
          <h1 className="text-lg font-bold tracking-tight bg-linear-to-r from-violet-400 via-indigo-400 to-purple-400 bg-clip-text text-transparent">Marginalia</h1>
        </Link>
      </div>

      <div className="flex items-center gap-2">
        {documentId && filename && (
          <>
            <ExportControls documentId={documentId} filename={filename} />
            <Separator orientation="vertical" className="h-6" />
          </>
        )}

        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              variant="ghost"
              size="sm"
              className="gap-2"
              onClick={handleNewSession}
            >
              <PlusCircle className="h-4 w-4" aria-hidden="true" />
              <span className="hidden sm:inline">New</span>
            </Button>
          </TooltipTrigger>
          <TooltipContent>Start a new editing session</TooltipContent>
        </Tooltip>

        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              onClick={() => setIsConfigOpen(true)}
              aria-label="Open model settings"
            >
              <Settings className="h-4 w-4" />
            </Button>
          </TooltipTrigger>
          <TooltipContent>Model settings</TooltipContent>
        </Tooltip>

        <LlmConfigDialog
          open={isConfigOpen}
          onOpenChange={setIsConfigOpen}
          config={llmConfig}
          isLoading={isConfigLoading}
          isCheckingHealth={isCheckingHealth}
          healthResult={healthResult}
          onCheckHealth={onCheckHealth}
        />
      </div>
    </header>
  );
}
