import { useState, useCallback, useRef, useEffect } from "react";
import { Card, CardContent, CardFooter, CardHeader } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Textarea } from "@/components/ui/textarea";
import { Separator } from "@/components/ui/separator";
import {
  Check,
  X,
  Pencil,
  AlertCircle,
  ArrowRight,
  ChevronDown,
  ChevronUp,
} from "lucide-react";
import type { Suggestion, SuggestionStatus } from "@/types";
import { cn, mutedText } from "@/lib/utils";

interface SuggestionCardProps {
  suggestion: Suggestion;
  number?: number;
  isActive: boolean;
  isHovered?: boolean;
  onStatusChange: (
    id: string,
    status: SuggestionStatus,
    modifiedText?: string
  ) => void;
  onClick: (id: string) => void;
  onHoverChange?: (id: string | null) => void;
}

const statusBadge = {
  Pending: { variant: "default" as const, icon: AlertCircle, className: "bg-linear-to-r from-amber-500/90 to-orange-500/90 text-white border-0 shadow-sm" },
  Accepted: { variant: "secondary" as const, icon: Check, className: "bg-linear-to-r from-emerald-500/15 to-teal-500/15 text-emerald-700 dark:from-emerald-500/25 dark:to-teal-500/25 dark:text-emerald-300 border-0 shadow-sm" },
  Rejected: { variant: "destructive" as const, icon: X, className: "bg-linear-to-r from-rose-500/15 to-pink-500/15 text-rose-700 dark:from-rose-500/25 dark:to-pink-500/25 dark:text-rose-300 border-0 shadow-sm" },
  Modified: { variant: "outline" as const, icon: Pencil, className: "bg-linear-to-r from-sky-500/15 to-indigo-500/15 text-sky-700 dark:from-sky-500/25 dark:to-indigo-500/25 dark:text-sky-300 border-0 shadow-sm" },
};

const statusNumberColors = {
  Pending: "bg-amber-500 text-white",
  Accepted: "bg-emerald-500 text-white",
  Rejected: "bg-rose-500 text-white",
  Modified: "bg-sky-500 text-white",
} as const;

export function SuggestionCard({
  suggestion,
  number,
  isActive,
  isHovered,
  onStatusChange,
  onClick,
  onHoverChange,
}: SuggestionCardProps) {
  const cardRef = useRef<HTMLDivElement>(null);
  const [isEditing, setIsEditing] = useState(false);
  const [modifiedText, setModifiedText] = useState(suggestion.proposedChange);
  const [isExpanded, setIsExpanded] = useState(false);

  useEffect(() => {
    if (isActive && cardRef.current) {
      cardRef.current.scrollIntoView({ behavior: "smooth", block: "nearest" });
    }
  }, [isActive]);

  const badge = statusBadge[suggestion.status];
  const BadgeIcon = badge.icon;

  const handleAccept = useCallback(() => {
    onStatusChange(suggestion.id, "Accepted");
  }, [onStatusChange, suggestion.id]);

  const handleReject = useCallback(() => {
    onStatusChange(suggestion.id, "Rejected");
  }, [onStatusChange, suggestion.id]);

  const handleModify = useCallback(() => {
    if (isEditing) {
      onStatusChange(suggestion.id, "Modified", modifiedText);
      setIsEditing(false);
    } else {
      setIsEditing(true);
    }
  }, [isEditing, modifiedText, onStatusChange, suggestion.id]);

  const handleClick = useCallback(() => {
    onClick(suggestion.id);
  }, [onClick, suggestion.id]);

  return (
    <Card
      ref={cardRef}
      className={`transition-all duration-200 ${
        isActive ? "ring-2 ring-primary shadow-lg scale-[1.01]" : isHovered ? "ring-1 ring-primary/40 shadow-md" : "hover:shadow-md hover:border-muted-foreground/20"
      }`}
      role="article"
      aria-label={`Suggestion${number != null ? ` ${number}` : ""}: ${suggestion.rationale.slice(0, 50)}`}
      onMouseEnter={() => onHoverChange?.(suggestion.id)}
      onMouseLeave={() => onHoverChange?.(null)}
    >
      <CardHeader
        className="pb-2 cursor-pointer"
        onClick={handleClick}
        onKeyDown={(e: React.KeyboardEvent) => {
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            handleClick();
          }
        }}
        tabIndex={0}
        role="button"
        aria-expanded={isExpanded}
      >
        <div className="flex items-start justify-between gap-2">
          <div className="flex items-center gap-2">
            {number != null && (
              <span
                className={`inline-flex items-center justify-center w-6 h-6 rounded-full text-xs font-bold shrink-0 ${statusNumberColors[suggestion.status]}`}
                aria-hidden="true"
              >
                {number}
              </span>
            )}
            <Badge variant={badge.variant} className={`gap-1 shrink-0 ${badge.className}`}>
              <BadgeIcon className="h-3 w-3" aria-hidden="true" />
              {suggestion.status}
            </Badge>
          </div>
          <Button
            variant="ghost"
            size="sm"
            className="h-6 w-6 p-0"
            onClick={(e: React.MouseEvent) => {
              e.stopPropagation();
              setIsExpanded(!isExpanded);
            }}
            aria-label={isExpanded ? "Collapse suggestion" : "Expand suggestion"}
          >
            {isExpanded ? (
              <ChevronUp className="h-4 w-4" />
            ) : (
              <ChevronDown className="h-4 w-4" />
            )}
          </Button>
        </div>
        <p className={cn(mutedText, "mt-1")}>
          {suggestion.rationale}
        </p>
      </CardHeader>

      {isExpanded && (
        <>
          <Separator />
          <CardContent className="pt-3">
            <div className="flex items-start gap-2 text-sm">
              <ArrowRight
                className="h-4 w-4 mt-0.5 shrink-0 text-muted-foreground"
                aria-hidden="true"
              />
              <div className="flex-1">
                <span className="text-xs font-medium text-muted-foreground block mb-1">
                  Proposed change:
                </span>
                {isEditing ? (
                  <Textarea
                    value={modifiedText}
                    onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => setModifiedText(e.target.value)}
                    rows={3}
                    className="text-sm"
                    aria-label="Modified suggestion text"
                  />
                ) : (
                  <p className="text-sm bg-muted/50 rounded-md p-2">
                    {suggestion.proposedChange}
                  </p>
                )}
              </div>
            </div>
          </CardContent>

          {suggestion.status === "Pending" && (
            <CardFooter className="gap-2 pt-0">
              <Button
                size="sm"
                variant="accept"
                className="gap-1"
                onClick={handleAccept}
              >
                <Check className="h-3 w-3" aria-hidden="true" />
                Accept
              </Button>
              <Button
                size="sm"
                variant="destructive"
                className="gap-1"
                onClick={handleReject}
              >
                <X className="h-3 w-3" aria-hidden="true" />
                Reject
              </Button>
              <Button
                size="sm"
                variant="outline"
                className="gap-1"
                onClick={handleModify}
              >
                <Pencil className="h-3 w-3" aria-hidden="true" />
                {isEditing ? "Save" : "Modify"}
              </Button>
              {isEditing && (
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => {
                    setIsEditing(false);
                    setModifiedText(suggestion.proposedChange);
                  }}
                >
                  Cancel
                </Button>
              )}
            </CardFooter>
          )}
        </>
      )}
    </Card>
  );
}
