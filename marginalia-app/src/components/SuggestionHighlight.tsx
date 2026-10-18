import { useCallback } from "react";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { Badge } from "@/components/ui/badge";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/utils";
import type { Suggestion } from "@/types";

interface SuggestionMarkerProps {
  suggestion: Suggestion;
  number: number;
  isActive: boolean;
  isHovered: boolean;
  onClick: (id: string) => void;
  onHoverChange: (id: string | null) => void;
}

const statusMarkerColors = {
  Pending: "bg-status-pending text-status-foreground",
  Accepted: "bg-status-accepted text-status-foreground",
  Rejected: "bg-status-rejected text-status-foreground",
  Modified: "bg-status-modified text-status-foreground",
} as const;

interface SuggestionMarkerGroupProps {
  markers: { suggestion: Suggestion; number: number }[];
  activeSuggestionId: string | null;
  hoveredSuggestionId: string | null;
  onClick: (id: string) => void;
  onHoverChange: (id: string | null) => void;
}

export function SuggestionMarkerGroup({
  markers, activeSuggestionId, hoveredSuggestionId, onClick, onHoverChange,
}: SuggestionMarkerGroupProps) {
  const first = markers[0];
  if (markers.length === 1) {
    return <SuggestionMarker {...first}
      isActive={first.suggestion.id === activeSuggestionId}
      isHovered={first.suggestion.id === hoveredSuggestionId}
      onClick={onClick} onHoverChange={onHoverChange} />;
  }
  const isHighlighted = markers.some(({ suggestion }) =>
    suggestion.id === activeSuggestionId || suggestion.id === hoveredSuggestionId);

  return (
    <DropdownMenu onOpenChange={(open) => { if (!open) onHoverChange(null); }}>
      <DropdownMenuTrigger asChild>
        <button type="button"
          className={cn("ml-0.5 inline-flex h-6 min-w-6 items-center justify-center rounded-full px-0.5 text-xs font-bold",
            statusMarkerColors[first.suggestion.status],
            isHighlighted && "ring-2 ring-primary ring-offset-1 ring-offset-background")}
          aria-label={`Suggestion ${first.number}, plus ${markers.length - 1} more for this paragraph`}>
          {first.number}+
        </button>
      </DropdownMenuTrigger>
      <DropdownMenuContent side="right" align="start" className="max-w-72">
        {markers.map(({ suggestion, number }) => (
          <DropdownMenuItem key={suggestion.id} className="min-h-11 items-start"
            onSelect={() => onClick(suggestion.id)}
            onFocus={() => onHoverChange(suggestion.id)}
            onMouseEnter={() => onHoverChange(suggestion.id)}
            onMouseLeave={() => onHoverChange(null)}>
            <span className={cn("inline-flex size-6 shrink-0 items-center justify-center rounded-full text-xs font-bold", statusMarkerColors[suggestion.status])}>
              {number}
            </span>
            <span><span className="block font-medium">{suggestion.status}</span>{suggestion.rationale}</span>
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

export function SuggestionMarker({
  suggestion,
  number,
  isActive,
  isHovered,
  onClick,
  onHoverChange,
}: SuggestionMarkerProps) {
  const colorClass = statusMarkerColors[suggestion.status];

  const handleClick = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      onClick(suggestion.id);
    },
    [onClick, suggestion.id]
  );

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <button
          type="button"
          className={`inline-flex items-center justify-center min-w-6 h-6 px-0.5 ml-0.5 rounded-full text-xs font-bold cursor-pointer transition-colors duration-150 ${colorClass} ${
            isActive
              ? "ring-2 ring-primary ring-offset-1 ring-offset-background"
              : isHovered
                ? "ring-2 ring-primary/50 ring-offset-1 ring-offset-background"
                : "hover:shadow-sm"
          }`}
          onClick={handleClick}
          onMouseEnter={() => onHoverChange(suggestion.id)}
          onMouseLeave={() => onHoverChange(null)}
          aria-label={`Suggestion ${number}, ${suggestion.status}: ${suggestion.rationale.slice(0, 60)}`}
        >
          {number}
        </button>
      </TooltipTrigger>
      <TooltipContent side="top" className="max-w-xs" role="tooltip">
        <div className="flex flex-col gap-1">
          <Badge
            variant={suggestion.status === "Pending" ? "default" : "secondary"}
            className="w-fit text-xs"
          >
            {suggestion.status}
          </Badge>
          <p className="text-sm">{suggestion.rationale}</p>
        </div>
      </TooltipContent>
    </Tooltip>
  );
}
