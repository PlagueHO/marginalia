import { useCallback } from "react";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { Badge } from "@/components/ui/badge";
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
  Pending: "bg-amber-500 text-white dark:bg-amber-700",
  Accepted: "bg-emerald-500 text-white dark:bg-emerald-500",
  Rejected: "bg-rose-500 text-white dark:bg-rose-400",
  Modified: "bg-sky-500 text-white dark:bg-sky-500",
} as const;

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
        <sup
          role="mark"
          className={`inline-flex items-center justify-center min-w-[1.1rem] h-[1.1rem] px-0.5 ml-0.5 rounded-full text-[10px] font-bold cursor-pointer transition-all duration-150 ${colorClass} ${
            isActive
              ? "ring-2 ring-primary ring-offset-1 ring-offset-background scale-125"
              : isHovered
                ? "ring-2 ring-primary/50 ring-offset-1 ring-offset-background scale-110"
                : "hover:scale-110 hover:shadow-sm"
          }`}
          onClick={handleClick}
          onMouseEnter={() => onHoverChange(suggestion.id)}
          onMouseLeave={() => onHoverChange(null)}
          onKeyDown={(e) => {
            if (e.key === "Enter" || e.key === " ") {
              e.preventDefault();
              onClick(suggestion.id);
            }
          }}
          tabIndex={0}
          aria-label={`Suggestion ${number}: ${suggestion.rationale.slice(0, 60)}`}
        >
          {number}
        </sup>
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
