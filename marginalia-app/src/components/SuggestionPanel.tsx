import { useCallback, useMemo } from "react";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Separator } from "@/components/ui/separator";
import { Button } from "@/components/ui/button";
import { Sparkles } from "lucide-react";
import { SuggestionCard } from "./SuggestionCard";
import { SuggestionBatchActions } from "./SuggestionBatchActions";
import { cn, mutedText } from "@/lib/utils";
import type { Suggestion, SuggestionStatus, Paragraph } from "@/types";

interface SuggestionPanelProps {
  suggestions: Suggestion[];
  filteredSuggestions: Suggestion[];
  filter: SuggestionStatus | "All";
  activeSuggestionId: string | null;
  hoveredSuggestionId: string | null;
  suggestionNumbers: Map<string, number>;
  paragraphs?: Paragraph[];
  counts: {
    Pending: number;
    Accepted: number;
    Rejected: number;
    Modified: number;
    total: number;
  };
  onFilterChange: (filter: SuggestionStatus | "All") => void;
  onStatusChange: (
    id: string,
    status: SuggestionStatus,
    modifiedText?: string
  ) => void;
  onSuggestionClick: (id: string) => void;
  onSuggestionHover: (id: string | null) => void;
  onAcceptAll: () => Promise<void>;
  onRejectAll: () => Promise<void>;
  onReanalyze?: (paragraphId: string) => void;
  isUnanalyzed?: boolean;
  onAnalyze?: () => void;
}

export function SuggestionPanel({
  filteredSuggestions,
  filter,
  activeSuggestionId,
  hoveredSuggestionId,
  suggestionNumbers,
  paragraphs,
  counts,
  onFilterChange,
  onStatusChange,
  onSuggestionClick,
  onSuggestionHover,
  onAcceptAll,
  onRejectAll,
  onReanalyze,
  isUnanalyzed,
  onAnalyze,
}: SuggestionPanelProps) {
  const handleStatusChange = useCallback(
    (id: string, status: SuggestionStatus, modifiedText?: string) => {
      onStatusChange(id, status, modifiedText);
    },
    [onStatusChange]
  );

  const paragraphRuns = useMemo(() => {
    const runs: Suggestion[][] = [];

    for (const suggestion of filteredSuggestions) {
      const currentRun = runs[runs.length - 1];

      if (
        currentRun == null ||
        currentRun[0]?.paragraphId !== suggestion.paragraphId
      ) {
        runs.push([suggestion]);
        continue;
      }

      currentRun.push(suggestion);
    }

    return runs;
  }, [filteredSuggestions]);

  return (
    <div className="flex h-full flex-col border-t bg-card/80 backdrop-blur-sm lg:border-l lg:border-t-0" role="complementary" aria-label="Suggestions panel">
      <div className="p-4 pb-2">
        <h3 className="text-sm font-semibold mb-3 text-center">Suggestions</h3>
        {!isUnanalyzed && (
          <SuggestionBatchActions
            counts={counts}
            onAcceptAll={onAcceptAll}
            onRejectAll={onRejectAll}
            isDisabled={false}
          />
        )}
      </div>

      <Separator />

      {isUnanalyzed ? (
        <div className="flex flex-col items-center justify-center flex-1 gap-4 p-8 text-center">
          <Sparkles className="h-8 w-8 text-violet-400/50" aria-hidden="true" />
          <p className={cn(mutedText, "text-sm max-w-[180px]")}>
            Analyze the manuscript to generate suggestions
          </p>
          {onAnalyze && (
            <Button onClick={onAnalyze} className="min-h-11 gap-2">
              <Sparkles className="h-4 w-4" aria-hidden="true" />
              Analyze
            </Button>
          )}
        </div>
      ) : (
        <Tabs
          value={filter}
          onValueChange={(val: string) =>
            onFilterChange(val as SuggestionStatus | "All")
          }
          className="flex-1 flex flex-col"
        >
        <TabsList className="mx-2 mt-2 grid h-auto min-h-11 grid-cols-5 justify-center sm:mx-4">
          <TabsTrigger value="All" className="min-h-10 px-1 text-[11px] data-[state=active]:shadow-sm sm:text-xs">
            All ({counts.total})
          </TabsTrigger>
          <TabsTrigger value="Pending" className="min-h-10 px-1 text-[11px] data-[state=active]:shadow-sm sm:text-xs">
            Pending
          </TabsTrigger>
          <TabsTrigger value="Accepted" className="min-h-10 px-1 text-[11px] data-[state=active]:shadow-sm sm:text-xs">
            ✓
          </TabsTrigger>
          <TabsTrigger value="Rejected" className="min-h-10 px-1 text-[11px] data-[state=active]:shadow-sm sm:text-xs">
            ✗
          </TabsTrigger>
          <TabsTrigger value="Modified" className="min-h-10 px-1 text-[11px] data-[state=active]:shadow-sm sm:text-xs">
            ✎
          </TabsTrigger>
        </TabsList>

        <TabsContent value={filter} className="flex-1 m-0 overflow-hidden">
          <ScrollArea className="h-full max-h-[calc(100vh-16rem)]">
            <div className="flex flex-col gap-3 p-4">
              {filteredSuggestions.length === 0 ? (
                <p className={cn(mutedText, "text-center py-8")}>
                  No {filter === "All" ? "" : filter.toLowerCase()} suggestions
                </p>
              ) : (
                paragraphRuns.map((run) => {
                  const hasSiblingSuggestions = run.length > 1;

                  return (
                    <div
                      key={run.map((suggestion) => suggestion.id).join("-")}
                      className={cn("relative", hasSiblingSuggestions && "pl-4")}
                    >
                      {hasSiblingSuggestions && (
                        <div
                          data-testid="paragraph-suggestion-connector"
                          className="absolute left-0 top-8 bottom-8 w-2 rounded-full bg-muted-foreground/20"
                          aria-hidden="true"
                        />
                      )}
                      <div className={cn("flex flex-col", hasSiblingSuggestions && "gap-3")}>
                        {run.map((suggestion) => (
                          <SuggestionCard
                            key={suggestion.id}
                            suggestion={suggestion}
                            number={suggestionNumbers.get(suggestion.id)}
                            isActive={suggestion.id === activeSuggestionId}
                            isHovered={suggestion.id === hoveredSuggestionId}
                            paragraphs={paragraphs}
                            onStatusChange={handleStatusChange}
                            onClick={onSuggestionClick}
                            onHoverChange={onSuggestionHover}
                            onReanalyze={onReanalyze}
                          />
                        ))}
                      </div>
                    </div>
                  );
                })
              )}
            </div>
          </ScrollArea>
        </TabsContent>
      </Tabs>
      )}
    </div>
  );
}
