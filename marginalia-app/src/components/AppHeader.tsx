import { useState } from "react";
import { Link, useNavigate, useLocation } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { cn, gradientText } from "@/lib/utils";
import { useTheme } from "@/hooks/useTheme";
import {
  BookOpen,
  CircleUser,
  FileText,
  Home,
  LogIn,
  Moon,
  PlusCircle,
  Settings,
  Sparkles,
  Sun,
  Trash2,
} from "lucide-react";
import { Separator } from "@/components/ui/separator";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
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
  onCheckHealth: () => Promise<void>;
  onAnalyze?: () => void;
  onDelete?: () => void;
}

export function AppHeader({
  documentId,
  filename,
  llmConfig,
  isConfigLoading,
  isCheckingHealth,
  healthResult,
  onCheckHealth,
  onAnalyze,
  onDelete,
}: AppHeaderProps) {
  const [isConfigOpen, setIsConfigOpen] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();
  const { theme, toggleTheme } = useTheme();
  const hasDocument = !!(documentId && filename);

  const isHome = location.pathname === "/";
  const isNew = location.pathname === "/new";
  const isEditor = location.pathname.startsWith("/editor/");

  const tabBase =
    "inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium rounded-md transition-colors cursor-pointer";
  const tabActive =
    "bg-accent text-foreground";
  const tabInactive =
    "text-muted-foreground hover:text-foreground hover:bg-accent/50";

  return (
    <header className="flex items-center justify-between px-4 py-2.5 border-b border-border/50 bg-linear-to-r from-background via-background to-muted/30 dark:from-zinc-950 dark:via-zinc-900/80 dark:to-zinc-800/40 backdrop-blur-md supports-backdrop-filter:bg-background/60 sticky top-0 z-50 shadow-sm">
      {/* Left: Brand + Navigation */}
      <div className="flex items-center gap-1">
        <Link
          to="/"
          className="flex items-center gap-2.5 hover:opacity-80 transition-opacity mr-2"
        >
          <BookOpen className="h-5 w-5 text-violet-400" aria-hidden="true" />
          <h1 className={cn(gradientText, "text-lg hidden sm:block")}>
            Marginalia
          </h1>
        </Link>

        <Separator orientation="vertical" className="h-6 mx-1" />

        <nav className="flex items-center gap-0.5" role="tablist" aria-label="Navigation">
          <button
            role="tab"
            aria-selected={isHome}
            className={`${tabBase} ${isHome ? tabActive : tabInactive}`}
            onClick={() => navigate("/")}
          >
            <Home className="h-4 w-4" aria-hidden="true" />
            <span className="hidden md:inline">Manuscripts</span>
          </button>

          <button
            role="tab"
            aria-selected={isNew}
            className={`${tabBase} ${isNew ? tabActive : tabInactive}`}
            onClick={() => navigate("/new")}
          >
            <PlusCircle className="h-4 w-4" aria-hidden="true" />
            <span className="hidden md:inline">New</span>
          </button>

          {hasDocument && (
            <button
              role="tab"
              aria-selected={isEditor}
              className={`${tabBase} ${isEditor ? tabActive : tabInactive}`}
              onClick={() => navigate(`/editor/${documentId}`)}
            >
              <FileText className="h-4 w-4" aria-hidden="true" />
              <span className="truncate max-w-[200px] hidden sm:inline" title={filename}>{filename}</span>
            </button>
          )}
        </nav>

        {hasDocument && (
          <>
            <Separator orientation="vertical" className="h-6 mx-1" />

            {onAnalyze && (
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button
                    size="sm"
                    className="gap-2 w-24 justify-center"
                    onClick={onAnalyze}
                    aria-label="Analyze manuscript"
                  >
                    <Sparkles className="h-4 w-4" aria-hidden="true" />
                    <span className="hidden md:inline">Analyze</span>
                  </Button>
                </TooltipTrigger>
                <TooltipContent>Run AI analysis on manuscript</TooltipContent>
              </Tooltip>
            )}

            <Tooltip>
              <TooltipTrigger asChild>
                <span>
                  <ExportControls documentId={documentId} filename={filename} />
                </span>
              </TooltipTrigger>
              <TooltipContent>Export to Word document</TooltipContent>
            </Tooltip>

            {onDelete && (
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button
                    variant="destructive"
                    size="sm"
                    className="gap-2 w-24 justify-center"
                    onClick={onDelete}
                    aria-label="Delete manuscript"
                  >
                    <Trash2 className="h-4 w-4" aria-hidden="true" />
                    <span className="hidden md:inline">Delete</span>
                  </Button>
                </TooltipTrigger>
                <TooltipContent>Delete manuscript</TooltipContent>
              </Tooltip>
            )}
          </>
        )}
      </div>

      {/* Right: Theme toggle + User Menu */}
      <div className="flex items-center gap-2">
        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              aria-label={theme === "dark" ? "Switch to light mode" : "Switch to dark mode"}
              onClick={toggleTheme}
            >
              {theme === "dark" ? (
                <Sun className="h-5 w-5" aria-hidden="true" />
              ) : (
                <Moon className="h-5 w-5" aria-hidden="true" />
              )}
            </Button>
          </TooltipTrigger>
          <TooltipContent>{theme === "dark" ? "Light mode" : "Dark mode"}</TooltipContent>
        </Tooltip>

        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              aria-label="User menu"
              className="rounded-full"
            >
              <CircleUser className="h-5 w-5" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-48">
            <DropdownMenuItem
              onClick={() => setIsConfigOpen(true)}
              className="gap-2"
            >
              <Settings className="h-4 w-4" aria-hidden="true" />
              Settings
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem disabled className="gap-2">
              <LogIn className="h-4 w-4" aria-hidden="true" />
              Sign In
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>

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
