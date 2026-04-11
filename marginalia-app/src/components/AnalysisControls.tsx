import { useState, useCallback } from "react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Card, CardContent } from "@/components/ui/card";
import { Sparkles, ChevronDown } from "lucide-react";
import { Spinner } from "@/components/ui/spinner";

interface AnalysisControlsProps {
  documentId: string;
  selectedText?: string;
  isAnalyzing: boolean;
  progress: string;
  onAnalyze: (guidance?: string, tone?: string) => void;
}

const TONE_OPTIONS = [
  { value: "professional", label: "Professional" },
  { value: "narrative", label: "Narrative" },
  { value: "academic", label: "Academic" },
  { value: "conversational", label: "Conversational" },
  { value: "literary", label: "Literary" },
] as const;

export function AnalysisControls({
  selectedText,
  isAnalyzing,
  progress,
  onAnalyze,
}: AnalysisControlsProps) {
  const [guidance, setGuidance] = useState("");
  const [tone, setTone] = useState<string>("");

  const handleAnalyze = useCallback(() => {
    onAnalyze(
      guidance.trim() || undefined,
      tone || undefined
    );
  }, [guidance, tone, onAnalyze]);

  const selectedToneLabel =
    TONE_OPTIONS.find((t) => t.value === tone)?.label ?? "Select tone";

  return (
    <Card className="border-t rounded-none border-x-0 border-b-0 bg-linear-to-t from-muted/20 to-transparent">
      <CardContent className="p-4">
        <div className="flex flex-col gap-3">
          <div className="flex flex-col sm:flex-row gap-3">
            <div className="flex-1">
              <Label htmlFor="guidance-input" className="text-xs font-medium mb-1 block">
                Guidance (optional)
              </Label>
              <Textarea
                id="guidance-input"
                placeholder="e.g., Focus on narrative flow, reduce compressed prose, expand with more sensory detail…"
                value={guidance}
                onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => setGuidance(e.target.value)}
                rows={2}
                className="resize-none text-sm"
                disabled={isAnalyzing}
              />
            </div>

            <div className="flex flex-col gap-2 sm:w-48">
              <Label className="text-xs font-medium">Tone</Label>
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button
                    variant="outline"
                    className="justify-between gap-2"
                    disabled={isAnalyzing}
                  >
                    {selectedToneLabel}
                    <ChevronDown className="h-4 w-4 opacity-50" aria-hidden="true" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="start" className="w-48">
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
            </div>
          </div>

          {selectedText && (
            <p className="text-xs text-muted-foreground">
              Analyzing selected text ({selectedText.length} characters)
            </p>
          )}

          {progress && (
            <p className="text-xs text-muted-foreground animate-pulse">
              {progress}
            </p>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
