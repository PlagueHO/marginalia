import { useState, useCallback, useEffect, useRef } from "react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { gradientText, mutedText } from "@/lib/utils";
import { Spinner } from "@/components/ui/spinner";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Sparkles, ChevronDown } from "lucide-react";

interface AnalysisDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  isAnalyzing: boolean;
  progress: string;
  onAnalyze: (guidance?: string, tone?: string) => void;
  paragraphMode?: boolean;
}

const TONE_OPTIONS = [
  { value: "professional", label: "Professional" },
  { value: "narrative", label: "Narrative" },
  { value: "academic", label: "Academic" },
  { value: "conversational", label: "Conversational" },
  { value: "literary", label: "Literary" },
] as const;

export function AnalysisDialog({
  open,
  onOpenChange,
  isAnalyzing,
  progress,
  onAnalyze,
  paragraphMode = false,
}: AnalysisDialogProps) {
  const [guidance, setGuidance] = useState("");
  const [tone, setTone] = useState<string>("");
  const wasAnalyzingRef = useRef(false);

  // Auto-close the dialog when analysis completes
  useEffect(() => {
    if (wasAnalyzingRef.current && !isAnalyzing) {
      onOpenChange(false);
    }
    wasAnalyzingRef.current = isAnalyzing;
  }, [isAnalyzing, onOpenChange]);

  const handleAnalyze = useCallback(() => {
    onAnalyze(guidance.trim() || undefined, tone || undefined);
  }, [guidance, tone, onAnalyze]);

  const selectedToneLabel =
    TONE_OPTIONS.find((t) => t.value === tone)?.label ?? "Select tone";

  return (
    <Dialog open={open} onOpenChange={isAnalyzing ? undefined : onOpenChange}>
      <DialogContent
        className="sm:max-w-md"
        aria-describedby="analysis-dialog-description"
        onInteractOutside={(e) => {
          if (isAnalyzing) e.preventDefault();
        }}
        onEscapeKeyDown={(e) => {
          if (isAnalyzing) e.preventDefault();
        }}
      >
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Sparkles className="h-5 w-5 text-violet-400" aria-hidden="true" />
            <span className={gradientText}>
              {paragraphMode ? "Analyze Paragraph" : "Analyze Manuscript"}
            </span>
          </DialogTitle>
          <DialogDescription id="analysis-dialog-description">
            {paragraphMode
              ? "Re-analyze this paragraph with updated guidance to generate a new suggestion."
              : "Configure analysis options and run AI-powered suggestions on your manuscript."}
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-4 py-2">
          <div className="flex flex-col gap-2">
            <Label className="text-sm font-medium">Tone</Label>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="outline"
                  className="justify-between gap-2 w-full"
                  disabled={isAnalyzing}
                >
                  {selectedToneLabel}
                  <ChevronDown className="h-4 w-4 opacity-50" aria-hidden="true" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="start" className="w-(--radix-dropdown-menu-trigger-width)">
                <DropdownMenuItem onClick={() => setTone("")}>
                  No preference
                </DropdownMenuItem>
                {TONE_OPTIONS.map((opt) => (
                  <DropdownMenuItem
                    key={opt.value}
                    onClick={() => setTone(opt.value)}
                  >
                    {opt.label}
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>
          </div>

          <div className="flex flex-col gap-2">
            <Label htmlFor="analysis-guidance-input" className="text-sm font-medium">
              Guidance (optional)
            </Label>
            <Textarea
              id="analysis-guidance-input"
              placeholder="e.g., Focus on narrative flow, reduce compressed prose, expand with more sensory detail…"
              value={guidance}
              onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) =>
                setGuidance(e.target.value)
              }
              rows={3}
              className="resize-none text-sm"
              disabled={isAnalyzing}
            />
          </div>

          {isAnalyzing && progress && (
            <div className="flex items-center gap-3 rounded-md border bg-muted/50 px-4 py-3">
              <Spinner className="text-violet-400 shrink-0" />
              <p className={mutedText}>{progress}</p>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={isAnalyzing}
          >
            Cancel
          </Button>
          <Button
            onClick={handleAnalyze}
            disabled={isAnalyzing}
            className="gap-2"
          >
            {isAnalyzing ? (
              <Spinner />
            ) : (
              <Sparkles className="h-4 w-4" aria-hidden="true" />
            )}
            {isAnalyzing ? "Analyzing…" : "Analyze"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
