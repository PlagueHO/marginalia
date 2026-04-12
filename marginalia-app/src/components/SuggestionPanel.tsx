import { useCallback } from "react";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Separator } from "@/components/ui/separator";
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
}: SuggestionPanelProps) {
  const handleStatusChange = useCallback(
    (id: string, status: SuggestionStatus, modifiedText?: string) => {
      onStatusChange(id, status, modifiedText);
    },
    [onStatusChange]
  );

  return (
    <div className="flex flex-col h-full border-l bg-card/80 backdrop-blur-sm" role="complementary" aria-label="Suggestions panel">
      <div className="p-4 pb-2">
        <h3 className="text-sm font-semibold mb-3 text-center">Suggestions</h3>
        <SuggestionBatchActions
          counts={counts}
          onAcceptAll={onAcceptAll}
          onRejectAll={onRejectAll}
          isDisabled={false}
        />
      </div>

      <Separator />

      <Tabs
        value={filter}
        onValueChange={(val: string) =>
          onFilterChange(val as SuggestionStatus | "All")
        }
        className="flex-1 flex flex-col"
      >
        <TabsList className="mx-4 mt-2 grid grid-cols-5 h-9 justify-center">
          <TabsTrigger value="All" className="text-xs data-[state=active]:shadow-sm">
            All ({counts.total})
          </TabsTrigger>
          <TabsTrigger value="Pending" className="text-xs data-[state=active]:shadow-sm">
            Pending
          </TabsTrigger>
          <TabsTrigger value="Accepted" className="text-xs data-[state=active]:shadow-sm">
            ✓
          </TabsTrigger>
          <TabsTrigger value="Rejected" className="text-xs data-[state=active]:shadow-sm">
            ✗
          </TabsTrigger>
          <TabsTrigger value="Modified" className="text-xs data-[state=active]:shadow-sm">
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
                filteredSuggestions.map((suggestion) => (
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
                ))
              )}
            </div>
          </ScrollArea>
        </TabsContent>
      </Tabs>
    </div>
  );
}
