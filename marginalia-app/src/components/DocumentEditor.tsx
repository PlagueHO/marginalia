import { useMemo, useCallback, useRef, type ReactNode } from "react";
import { ScrollArea } from "@/components/ui/scroll-area";
import { SuggestionMarker } from "./SuggestionHighlight";
import type { Document, Suggestion } from "@/types";

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
    "bg-amber-200/70 dark:bg-amber-400/20 border-b-2 border-amber-500 dark:border-amber-400",
  Accepted:
    "bg-emerald-200/70 dark:bg-emerald-400/20 border-b-2 border-emerald-500 dark:border-emerald-400",
  Rejected:
    "bg-rose-200/70 dark:bg-rose-400/20 border-b-2 border-rose-400 dark:border-rose-400 line-through opacity-70",
  Modified:
    "bg-sky-200/70 dark:bg-sky-400/20 border-b-2 border-sky-500 dark:border-sky-400",
} as const;

interface Segment {
  text: string;
  activeSuggestions: Suggestion[];
  endingMarkers: { suggestion: Suggestion; number: number }[];
}

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

  const segments = useMemo((): Segment[] => {
    const content = document.content;
    if (suggestions.length === 0) {
      return [{ text: content, activeSuggestions: [], endingMarkers: [] }];
    }

    // Detect paragraph breaks (double newline, possibly with \r)
    function isParagraphBreak(pos: number): boolean {
      if (pos >= content.length) return false;
      if (content[pos] === "\n") {
        const next = pos + 1;
        if (next < content.length && content[next] === "\n") return true;
        if (
          next < content.length &&
          content[next] === "\r" &&
          next + 1 < content.length &&
          content[next + 1] === "\n"
        )
          return true;
      }
      if (content[pos] === "\r" && pos + 1 < content.length && content[pos + 1] === "\n") {
        return isParagraphBreak(pos + 1);
      }
      return false;
    }

    // Expand a range end forward to the nearest sentence-ending punctuation
    // or paragraph break. Only ever returns a value >= pos (expands outward).
    function expandEndToSentenceBoundary(pos: number): number {
      const clamped = Math.max(0, Math.min(pos, content.length));
      if (clamped >= content.length) return content.length;

      // Already at a sentence boundary?
      if (clamped > 0 && /[.!?:"\u201D]/.test(content[clamped - 1]))
        return clamped;

      // Scan forward for the next sentence-ending punctuation or paragraph break
      let end = clamped;
      while (end < content.length) {
        if (/[.!?]/.test(content[end])) {
          let boundary = end + 1;
          while (
            boundary < content.length &&
            /["\u201D)\]]/.test(content[boundary])
          ) {
            boundary++;
          }
          return boundary;
        }
        if (isParagraphBreak(end)) return end;
        end++;
      }
      return content.length;
    }

    // Expand a range start backward to the nearest sentence boundary
    // or paragraph break. Only ever returns a value <= pos (expands outward).
    function expandStartToSentenceBoundary(pos: number): number {
      const clamped = Math.max(0, Math.min(pos, content.length));
      if (clamped === 0) return 0;

      // Already at start of a sentence? (preceded by paragraph break or sentence-end + space)
      if (clamped >= 2) {
        let check = clamped;
        // Back up through whitespace
        while (check > 0 && /[\s]/.test(content[check - 1])) check--;
        if (check === 0 || /[.!?:"\u201D]/.test(content[check - 1]))
          return check === 0 ? 0 : clamped;
      }

      // Scan backward for sentence-ending punctuation or paragraph break
      let start = clamped - 1;
      while (start > 0) {
        if (isParagraphBreak(start)) {
          // Skip past the paragraph break characters to the start of next paragraph
          let boundary = start;
          while (boundary < clamped && /[\r\n\s]/.test(content[boundary]))
            boundary++;
          return boundary;
        }
        if (/[.!?]/.test(content[start])) {
          // Advance past closing quotes
          let boundary = start + 1;
          while (
            boundary < clamped &&
            /["\u201D)\]]/.test(content[boundary])
          ) {
            boundary++;
          }
          // Skip whitespace after punctuation
          while (boundary < clamped && /[\s]/.test(content[boundary]))
            boundary++;
          // Ensure we never move past the original position
          return Math.min(boundary, clamped);
        }
        start--;
      }
      return 0;
    }

    // Build adjusted ranges expanded to sentence boundaries
    const adjustedRanges = new Map<string, { start: number; end: number }>();
    for (const s of suggestions) {
      const rawStart = Math.max(0, Math.min(s.textRange.start, content.length));
      const rawEnd = Math.max(0, Math.min(s.textRange.end, content.length));
      const snappedStart = expandStartToSentenceBoundary(rawStart);
      const snappedEnd = expandEndToSentenceBoundary(rawEnd);
      adjustedRanges.set(s.id, {
        // Guarantee start <= end; never contract the range
        start: Math.min(snappedStart, rawStart),
        end: Math.max(snappedEnd, rawEnd),
      });
    }

    // Collect all breakpoint positions where any suggestion starts or ends
    const breakpointSet = new Set<number>();
    breakpointSet.add(0);
    breakpointSet.add(content.length);

    for (const range of adjustedRanges.values()) {
      breakpointSet.add(range.start);
      breakpointSet.add(range.end);
    }

    const breakpoints = Array.from(breakpointSet).sort((a, b) => a - b);

    const result: Segment[] = [];
    for (let i = 0; i < breakpoints.length - 1; i++) {
      const segStart = breakpoints[i];
      const segEnd = breakpoints[i + 1];
      if (segStart >= segEnd) continue;

      // All suggestions that fully cover this segment (using adjusted ranges)
      const active = suggestions.filter((s) => {
        const range = adjustedRanges.get(s.id)!;
        return range.start <= segStart && range.end >= segEnd;
      });

      // Suggestions that end at this segment boundary (place numbered markers here)
      const ending = active
        .filter((s) => adjustedRanges.get(s.id)!.end === segEnd)
        .map((s) => ({
          suggestion: s,
          number: suggestionNumbers.get(s.id) ?? 0,
        }))
        .sort((a, b) => a.number - b.number);

      result.push({
        text: content.slice(segStart, segEnd),
        activeSuggestions: active,
        endingMarkers: ending,
      });
    }

    return result;
  }, [document.content, suggestions, suggestionNumbers]);

  const renderSegment = useCallback(
    (segment: Segment, index: number): ReactNode => {
      if (segment.activeSuggestions.length === 0) {
        return <span key={index}>{segment.text}</span>;
      }

      // Determine highlight style from the primary (lowest-numbered) suggestion
      const primary = segment.activeSuggestions.reduce((best, s) => {
        const bestNum = suggestionNumbers.get(best.id) ?? Infinity;
        const sNum = suggestionNumbers.get(s.id) ?? Infinity;
        return sNum < bestNum ? s : best;
      }, segment.activeSuggestions[0]);

      const highlightClass = statusHighlightClass[primary.status];
      const isActive = segment.activeSuggestions.some(
        (s) => s.id === activeSuggestionId
      );
      const isHovered = segment.activeSuggestions.some(
        (s) => s.id === hoveredSuggestionId
      );

      return (
        <span key={index}>
          <span
            className={`${highlightClass} cursor-pointer rounded-sm px-0.5 transition-all duration-200 inline ${
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
            {segment.text}
          </span>
          {segment.endingMarkers.map((marker) => (
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
        </span>
      );
    },
    [
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
        className="p-6 md:p-8 max-w-none prose dark:prose-invert prose-sm sm:prose-base leading-relaxed whitespace-pre-wrap font-serif"
        role="document"
        aria-label={`Document: ${document.filename}`}
      >
        {segments.map(renderSegment)}
      </div>
    </ScrollArea>
  );
}
