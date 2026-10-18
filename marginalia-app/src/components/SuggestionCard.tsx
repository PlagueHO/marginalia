import { useState, useCallback, useRef, useEffect, useId } from "react";
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
  Undo2,
  Sparkles,
} from "lucide-react";
import type { Suggestion, SuggestionStatus, Paragraph } from "@/types";
import { cn, mutedText } from "@/lib/utils";

interface SuggestionCardProps {
  suggestion: Suggestion;
  number?: number;
  isActive: boolean;
  isHovered?: boolean;
  paragraphs?: Paragraph[];
  onStatusChange: (
    id: string,
    status: SuggestionStatus,
    modifiedText?: string
  ) => void;
  onClick: (id: string) => void;
  onHoverChange?: (id: string | null) => void;
  onReanalyze?: (paragraphId: string) => void;
}

const statusBadge = {
  Pending: { variant: "outline" as const, icon: AlertCircle, className: "bg-status-pending/10 text-status-pending border-transparent" },
  Accepted: { variant: "outline" as const, icon: Check, className: "bg-status-accepted/10 text-status-accepted border-transparent" },
  Rejected: { variant: "outline" as const, icon: X, className: "bg-status-rejected/10 text-status-rejected border-transparent" },
  Modified: { variant: "outline" as const, icon: Pencil, className: "bg-status-modified/10 text-status-modified border-transparent" },
};

const statusNumberColors = {
  Pending: "bg-status-pending text-status-foreground",
  Accepted: "bg-status-accepted text-status-foreground",
  Rejected: "bg-status-rejected text-status-foreground",
  Modified: "bg-status-modified text-status-foreground",
} as const;

export function SuggestionCard({
  suggestion,
  number,
  isActive,
  isHovered,
  paragraphs,
  onStatusChange,
  onClick,
  onHoverChange,
  onReanalyze,
}: SuggestionCardProps) {
  const cardRef = useRef<HTMLDivElement>(null);
  const contentId = useId();
  const [isEditing, setIsEditing] = useState(false);
  const [modifiedText, setModifiedText] = useState(suggestion.proposedChange);
  const [isExpanded, setIsExpanded] = useState(false);
  const [userCollapsed, setUserCollapsed] = useState(false);

  // Reset the user-collapsed override whenever this suggestion is newly selected.
  // Using React's render-time derived state pattern to avoid setState-in-effect lint errors.
  const [prevIsActive, setPrevIsActive] = useState(isActive);
  if (isActive !== prevIsActive) {
    setPrevIsActive(isActive);
    if (isActive) setUserCollapsed(false);
  }

  useEffect(() => {
    if (isActive && cardRef.current) {
      cardRef.current.scrollIntoView({
        behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches ? "instant" : "smooth",
        block: "nearest",
      });
    }
  }, [isActive]);

  // Open when explicitly expanded or auto-opened by selection, unless user manually collapsed.
  const isOpen = (isExpanded || isActive) && !userCollapsed;

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

  const handleRevertToPending = useCallback(() => {
    onStatusChange(suggestion.id, "Pending");
  }, [onStatusChange, suggestion.id]);

  const handleReanalyze = useCallback(() => {
    onReanalyze?.(suggestion.paragraphId);
  }, [onReanalyze, suggestion.paragraphId]);

  const handleClick = useCallback(() => {
    onClick(suggestion.id);
  }, [onClick, suggestion.id]);

  return (
    <Card
      ref={cardRef}
      className={`transition-all duration-200 ${
        isActive ? "ring-2 ring-primary shadow-sm" : isHovered ? "ring-1 ring-primary/40" : "hover:border-input"
      }`}
      role="article"
      aria-label={`Suggestion${number != null ? ` ${number}` : ""}: ${suggestion.rationale.slice(0, 50)}`}
      onMouseEnter={() => onHoverChange?.(suggestion.id)}
      onMouseLeave={() => onHoverChange?.(null)}
    >
      <CardHeader className="pb-2">
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
            size="icon-sm"
            aria-expanded={isOpen}
            aria-controls={isOpen ? contentId : undefined}
            onClick={(e: React.MouseEvent) => {
              e.stopPropagation();
              if (isOpen) {
                setIsExpanded(false);
                setUserCollapsed(true);
              } else {
                setIsExpanded(true);
                setUserCollapsed(false);
              }
            }}
              aria-label={isOpen ? "Collapse suggestion" : "Expand suggestion"}
          >
            {isOpen ? (
              <ChevronUp className="h-4 w-4" aria-hidden="true" />
            ) : (
              <ChevronDown className="h-4 w-4" aria-hidden="true" />
            )}
          </Button>
        </div>
        <button type="button" onClick={handleClick}
          className={cn(mutedText, "mt-1 rounded-sm text-left hover:text-foreground")}>
          {suggestion.rationale}
        </button>
      </CardHeader>

      {isOpen && (
        <div id={contentId} className="flex flex-col gap-4">
          <Separator />
          <CardContent className="pt-3">
            <div className="flex items-start gap-2 text-sm">
              <ArrowRight
                className="h-4 w-4 mt-0.5 shrink-0 text-muted-foreground"
                aria-hidden="true"
              />
              <div className="flex-1">
                {paragraphs && (
                  <>
                    <span className="text-xs font-medium text-muted-foreground block mb-1">
                      Original text:
                    </span>
                    <p className="text-sm bg-muted/50 rounded-md p-2 mb-3 line-through decoration-rose-400/50">
                      {paragraphs.find((p) => p.id === suggestion.paragraphId)?.text ?? ""}
                    </p>
                  </>
                )}
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
            <CardFooter className="gap-2 pt-0 flex-wrap">
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
              {onReanalyze && (
                <Button
                  size="sm"
                  className="gap-1"
                  onClick={handleReanalyze}
                >
                  <Sparkles className="h-3 w-3" aria-hidden="true" />
                  Analyze
                </Button>
              )}
            </CardFooter>
          )}

          {(suggestion.status === "Accepted" || suggestion.status === "Rejected" || suggestion.status === "Modified") && (
            <CardFooter className="gap-2 pt-0 flex-wrap">
              <Button
                size="sm"
                variant="outline"
                className="gap-1"
                onClick={handleRevertToPending}
              >
                <Undo2 className="h-3 w-3" aria-hidden="true" />
                Revert to Pending
              </Button>
              {onReanalyze && (
                <Button
                  size="sm"
                  className="gap-1"
                  onClick={handleReanalyze}
                >
                  <Sparkles className="h-3 w-3" aria-hidden="true" />
                  Analyze
                </Button>
              )}
            </CardFooter>
          )}
        </div>
      )}
    </Card>
  );
}
