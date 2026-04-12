import { useMemo, useCallback, useRef, type ReactNode } from "react";
import { ScrollArea } from "@/components/ui/scroll-area";
import { SuggestionMarker } from "./SuggestionHighlight";
import type { Document, Suggestion, Paragraph } from "@/types";

interface DocumentEditorProps {
  document: Document;
  suggestions: Suggestion[];
  activeSuggestionId: string | null;
  hoveredSuggestionId: string | null;
  suggestionNumbers: Map<string, number>;
  onSuggestionClick: (id: string) => void;
  onSuggestionHover: (id: string | null) => void;
}

const statusHighlightClass = {
  Pending:
    "bg-amber-200/70 dark:bg-amber-500/45 border-b-2 border-amber-500 dark:border-amber-300",
  Accepted:
    "bg-emerald-200/70 dark:bg-emerald-500/45 border-b-2 border-emerald-500 dark:border-emerald-300",
  Rejected:
    "bg-rose-200/70 dark:bg-rose-500/45 border-b-2 border-rose-400 dark:border-rose-300",
  Modified:
    "bg-sky-200/70 dark:bg-sky-400/20 border-b-2 border-sky-500 dark:border-sky-400",
} as const;

export function DocumentEditor({
  document,
  suggestions,
  activeSuggestionId,
  hoveredSuggestionId,
  suggestionNumbers,
  onSuggestionClick,
  onSuggestionHover,
}: DocumentEditorProps) {
  const editorRef = useRef<HTMLDivElement>(null);

  // Group suggestions by paragraph ID
  const suggestionsByParagraph = useMemo(() => {
    const map = new Map<string, Suggestion[]>();
    for (const s of suggestions) {
      const list = map.get(s.paragraphId) ?? [];
      list.push(s);
      map.set(s.paragraphId, list);
    }
    return map;
  }, [suggestions]);

  const renderParagraph = useCallback(
    (paragraph: Paragraph): ReactNode => {
      const paragraphSuggestions = suggestionsByParagraph.get(paragraph.id) ?? [];

      // Find the primary suggestion (first pending, or first by number)
      const primary = paragraphSuggestions.length > 0
        ? paragraphSuggestions.reduce((best, s) => {
            const bestNum = suggestionNumbers.get(best.id) ?? Infinity;
            const sNum = suggestionNumbers.get(s.id) ?? Infinity;
            return sNum < bestNum ? s : best;
          }, paragraphSuggestions[0])
        : null;

      const isActive = paragraphSuggestions.some(
        (s) => s.id === activeSuggestionId
      );
      const isHovered = paragraphSuggestions.some(
        (s) => s.id === hoveredSuggestionId
      );

      if (!primary) {
        return (
          <div key={paragraph.id} className="relative rounded-md transition-colors duration-150 hover:bg-muted/30">
            <p>{paragraph.text}</p>
          </div>
        );
      }

      const highlightClass = statusHighlightClass[primary.status];
      const displayText =
        primary.status === "Accepted" ? primary.proposedChange : paragraph.text;

      const markers = paragraphSuggestions.map((s) => ({
        suggestion: s,
        number: suggestionNumbers.get(s.id) ?? 0,
      }));

      return (
        <div key={paragraph.id} className="relative rounded-md transition-colors duration-150 hover:bg-muted/30">
          <div className="absolute -left-8 top-1 flex flex-col gap-1">
            {markers.map((marker) => (
              <SuggestionMarker
                key={marker.suggestion.id}
                suggestion={marker.suggestion}
                number={marker.number}
                isActive={marker.suggestion.id === activeSuggestionId}
                isHovered={marker.suggestion.id === hoveredSuggestionId}
                onClick={onSuggestionClick}
                onHoverChange={onSuggestionHover}
              />
            ))}
          </div>
          <p
            className={`${highlightClass} cursor-pointer rounded-sm px-0.5 transition-all duration-200 ${
              isActive
                ? "ring-2 ring-primary ring-offset-2 ring-offset-background shadow-sm"
                : isHovered
                  ? "ring-1 ring-primary/40 ring-offset-1 ring-offset-background"
                  : ""
            }`}
            onClick={() => onSuggestionClick(primary.id)}
            onKeyDown={(e) => {
              if (e.key === "Enter" || e.key === " ") {
                e.preventDefault();
                onSuggestionClick(primary.id);
              }
            }}
            role="mark"
            tabIndex={0}
          >
            {displayText}
          </p>
        </div>
      );
    },
    [
      suggestionsByParagraph,
      activeSuggestionId,
      hoveredSuggestionId,
      onSuggestionClick,
      onSuggestionHover,
      suggestionNumbers,
    ]
  );

  return (
    <ScrollArea className="flex-1 h-full">
      <div
        ref={editorRef}
        className="py-6 px-4 ml-8 max-w-none prose dark:prose-invert prose-sm sm:prose-base leading-relaxed font-serif space-y-4"
        role="document"
        aria-label={`Document: ${document.filename}`}
      >
        {document.paragraphs.map(renderParagraph)}
      </div>
    </ScrollArea>
  );
}
