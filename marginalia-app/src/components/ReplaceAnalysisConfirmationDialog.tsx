import { useState, useCallback } from "react";
import type { Suggestion } from "@/types";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { AlertTriangle, ChevronDown, ChevronUp } from "lucide-react";
import { buildSummaryMessage } from "@/lib/replaceAnalysisSummary";

interface ReplaceAnalysisConfirmationDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  acceptedSuggestions: Suggestion[];
  pendingCount: number;
  rejectedCount: number;
  onConfirm: () => void;
}

export function ReplaceAnalysisConfirmationDialog({
  open,
  onOpenChange,
  acceptedSuggestions,
  pendingCount,
  rejectedCount,
  onConfirm,
}: ReplaceAnalysisConfirmationDialogProps) {
  const [isExpanded, setIsExpanded] = useState(true);

  const handleConfirm = useCallback(() => {
    onConfirm();
    onOpenChange(false);
  }, [onConfirm, onOpenChange]);

  const summary = buildSummaryMessage(
    acceptedSuggestions.length,
    pendingCount,
    rejectedCount,
  );

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="sm:max-w-md"
        aria-describedby="replace-analysis-dialog-description"
      >
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <AlertTriangle className="h-5 w-5 text-amber-500" aria-hidden="true" />
            Replace Analysis?
          </DialogTitle>
          <DialogDescription id="replace-analysis-dialog-description">
            This document has been analyzed before.
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-4 py-2">
          <Alert className="border-amber-200 bg-amber-50 dark:bg-amber-950/20">
            <AlertDescription className="text-amber-900 dark:text-amber-100">
              <div className="font-medium mb-1">
                {summary}
              </div>
            </AlertDescription>
          </Alert>

          {acceptedSuggestions.length > 0 && (
            <div className="flex flex-col gap-2">
              <button
                onClick={() => setIsExpanded(!isExpanded)}
                className="flex items-center justify-between px-3 py-2 rounded-md hover:bg-muted/50 transition-colors text-sm font-medium text-left"
              >
                <span>
                  Accepted Suggestions to Merge ({acceptedSuggestions.length})
                </span>
                {isExpanded ? (
                  <ChevronUp className="h-4 w-4 opacity-50" aria-hidden="true" />
                ) : (
                  <ChevronDown className="h-4 w-4 opacity-50" aria-hidden="true" />
                )}
              </button>

              {isExpanded && (
                <div className="border rounded-md overflow-hidden max-h-64 overflow-y-auto">
                  <div className="flex flex-col divide-y bg-muted/30">
                    {acceptedSuggestions.map((suggestion) => (
                      <div
                        key={suggestion.id}
                        className="px-3 py-2.5 text-sm hover:bg-muted/50 transition-colors"
                      >
                        <div className="flex items-start gap-2 mb-1">
                          <Badge className="mt-0.5 shrink-0" variant="secondary">
                            #{acceptedSuggestions.indexOf(suggestion) + 1}
                          </Badge>
                        </div>
                        <p className="text-xs line-clamp-2 text-muted-foreground">
                          {suggestion.proposedChange}
                        </p>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}
        </div>

        <DialogFooter className="flex gap-2">
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
          >
            Cancel
          </Button>
          <Button
            onClick={handleConfirm}
            className="bg-amber-600 hover:bg-amber-700"
          >
            Replace & Analyze
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

