import { useState, useCallback, useRef, useEffect } from "react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { FileText, Hash, Pencil, Check, X, Sparkles, Trash2 } from "lucide-react";
import { ExportControls } from "./ExportControls";
import { getAcceptedSuggestionsCharacterCount } from "@/lib/suggestionUtils";
import type { Document, Suggestion } from "@/types";

interface DocumentHeaderProps {
  document: Document;
  suggestions: Suggestion[];
  onRename?: (title: string) => Promise<void>;
  onAnalyze?: () => void;
  onDelete?: () => void;
}

export function DocumentHeader({ document, suggestions, onRename, onAnalyze, onDelete }: DocumentHeaderProps) {
  const [isEditing, setIsEditing] = useState(false);
  const [editValue, setEditValue] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);

  const displayTitle = document.title || document.filename;
  const originalCharacterCount = document.paragraphs.map((p) => p.text).join("\n\n").length;
  const withSuggestionsCharacterCount = getAcceptedSuggestionsCharacterCount(
    document.paragraphs,
    suggestions
  );

  const startEditing = useCallback(() => {
    setEditValue(displayTitle);
    setIsEditing(true);
  }, [displayTitle]);

  useEffect(() => {
    if (isEditing) {
      inputRef.current?.focus();
      inputRef.current?.select();
    }
  }, [isEditing]);

  const cancelEditing = useCallback(() => {
    setIsEditing(false);
    setEditValue("");
  }, []);

  const confirmEdit = useCallback(async () => {
    const trimmed = editValue.trim();
    if (!trimmed || trimmed === displayTitle) {
      cancelEditing();
      return;
    }
    if (onRename) {
      await onRename(trimmed);
    }
    setIsEditing(false);
  }, [editValue, displayTitle, onRename, cancelEditing]);

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent<HTMLInputElement>) => {
      if (e.key === "Enter") {
        void confirmEdit();
      } else if (e.key === "Escape") {
        cancelEditing();
      }
    },
    [confirmEdit, cancelEditing]
  );

  return (
    <div className="flex flex-wrap items-center gap-2 border-b bg-linear-to-r from-muted/20 via-muted/10 to-transparent px-3 py-2.5 sm:gap-3 sm:px-4">
      <FileText className="h-5 w-5 text-violet-400 shrink-0" aria-hidden="true" />
      {isEditing ? (
        <div className="flex items-center gap-1 flex-1 min-w-0">
          <input
            ref={inputRef}
            type="text"
            value={editValue}
            onChange={(e) => setEditValue(e.target.value)}
            onKeyDown={handleKeyDown}
            className="text-sm font-medium bg-background border border-input rounded-sm px-2 py-0.5 flex-1 min-w-0 focus:outline-none focus:ring-1 focus:ring-ring"
            aria-label="Edit document title"
          />
          <Button
            variant="ghost"
            size="icon"
            className="h-9 w-9 shrink-0 text-emerald-500 hover:text-emerald-600 hover:bg-emerald-500/10 sm:h-6 sm:w-6"
            onClick={() => void confirmEdit()}
            aria-label="Confirm title"
          >
            <Check className="h-3.5 w-3.5" />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            className="h-9 w-9 shrink-0 text-muted-foreground hover:text-foreground sm:h-6 sm:w-6"
            onClick={cancelEditing}
            aria-label="Cancel editing"
          >
            <X className="h-3.5 w-3.5" />
          </Button>
        </div>
      ) : (
        <div className="flex flex-1 min-w-0 items-center gap-1">
          <h2 className="text-sm font-medium truncate">{displayTitle}</h2>
          {onRename && (
            <Button
              variant="ghost"
              size="icon"
              className="h-9 w-9 shrink-0 text-muted-foreground hover:text-foreground sm:h-6 sm:w-6"
              onClick={startEditing}
              aria-label="Edit document title"
            >
              <Pencil className="h-3 w-3" />
            </Button>
          )}
        </div>
      )}
      <Badge variant="secondary" className="gap-1 shrink-0">
        <Hash className="h-3 w-3" aria-hidden="true" />
        {document.source}
      </Badge>
      <div className="flex w-full flex-wrap items-center gap-2 text-xs sm:ml-auto sm:w-auto">
        <span className="inline-flex items-center gap-1 rounded-full border border-border/70 bg-muted/50 px-2.5 py-1 text-muted-foreground">
          <Hash className="h-3 w-3" aria-hidden="true" />
          <span className="font-medium text-foreground">Original</span>
          <span>{originalCharacterCount.toLocaleString()}</span>
        </span>
        <span className="inline-flex items-center gap-1 rounded-full border border-emerald-300/40 bg-emerald-500/10 px-2.5 py-1 text-emerald-700 dark:text-emerald-300">
          <Check className="h-3 w-3" aria-hidden="true" />
          <span className="font-medium">With accepted</span>
          <span>{withSuggestionsCharacterCount.toLocaleString()}</span>
        </span>
        {onAnalyze && (
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                size="sm"
                className="min-h-11 gap-2 sm:min-h-8"
                onClick={onAnalyze}
                aria-label="Analyze manuscript"
              >
                <Sparkles className="h-4 w-4" aria-hidden="true" />
                Analyze
              </Button>
            </TooltipTrigger>
            <TooltipContent>Run AI analysis on manuscript</TooltipContent>
          </Tooltip>
        )}
        <Tooltip>
          <TooltipTrigger asChild>
            <span>
              <ExportControls documentId={document.id} filename={document.filename} />
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
                className="min-h-11 gap-2 sm:min-h-8"
                onClick={onDelete}
                aria-label="Delete manuscript"
              >
                <Trash2 className="h-4 w-4" aria-hidden="true" />
                Delete
              </Button>
            </TooltipTrigger>
            <TooltipContent>Delete manuscript</TooltipContent>
          </Tooltip>
        )}
      </div>
    </div>
  );
}
