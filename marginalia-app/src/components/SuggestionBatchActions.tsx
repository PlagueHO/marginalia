import { useCallback } from "react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { CheckCheck, XCircle, Clock, CheckCircle2, Ban, PenLine } from "lucide-react";

interface SuggestionBatchActionsProps {
  counts: {
    Pending: number;
    Accepted: number;
    Rejected: number;
    Modified: number;
    total: number;
  };
  onAcceptAll: () => Promise<void>;
  onRejectAll: () => Promise<void>;
  isDisabled: boolean;
}

export function SuggestionBatchActions({
  counts,
  onAcceptAll,
  onRejectAll,
  isDisabled,
}: SuggestionBatchActionsProps) {
  const handleAcceptAll = useCallback(async () => {
    await onAcceptAll();
  }, [onAcceptAll]);

  const handleRejectAll = useCallback(async () => {
    await onRejectAll();
  }, [onRejectAll]);

  const hasPending = counts.Pending > 0;

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap gap-2 justify-center">
        <Badge variant="secondary" className="bg-status-pending/10 text-status-pending border-0 pl-1.5">
          <Clock className="h-3 w-3" aria-hidden="true" />
          {counts.Pending} pending
        </Badge>
        <Badge variant="secondary" className="bg-status-accepted/10 text-status-accepted border-0 pl-1.5">
          <CheckCircle2 className="h-3 w-3" aria-hidden="true" />
          {counts.Accepted} accepted
        </Badge>
        <Badge variant="secondary" className="bg-status-rejected/10 text-status-rejected border-0 pl-1.5">
          <Ban className="h-3 w-3" aria-hidden="true" />
          {counts.Rejected} rejected
        </Badge>
        {counts.Modified > 0 && (
          <Badge variant="secondary" className="bg-status-modified/10 text-status-modified border-0 pl-1.5">
            <PenLine className="h-3 w-3" aria-hidden="true" />
            {counts.Modified} modified
          </Badge>
        )}
      </div>

      {hasPending && (
        <>
          <Separator />
          <div className="flex flex-col justify-center gap-2 sm:flex-row">
            <Button
              size="sm"
              variant="accept"
              className="gap-1 sm:flex-1"
              onClick={handleAcceptAll}
              disabled={isDisabled}
            >
              <CheckCheck className="h-3.5 w-3.5" aria-hidden="true" />
              Accept All ({counts.Pending})
            </Button>
            <Button
              size="sm"
              variant="destructive"
              className="gap-1 sm:flex-1"
              onClick={handleRejectAll}
              disabled={isDisabled}
            >
              <XCircle className="h-3.5 w-3.5" aria-hidden="true" />
              Reject All ({counts.Pending})
            </Button>
          </div>
        </>
      )}
    </div>
  );
}
