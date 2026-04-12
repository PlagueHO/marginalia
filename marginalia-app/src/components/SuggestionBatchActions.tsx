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
        <Badge variant="default" className="bg-linear-to-r from-amber-500/90 to-orange-500/90 dark:from-amber-700/95 dark:to-orange-700/95 text-white border-0 shadow-sm pl-1.5">
          <Clock className="h-3 w-3" aria-hidden="true" />
          {counts.Pending} pending
        </Badge>
        <Badge variant="secondary" className="bg-linear-to-r from-emerald-500/90 to-teal-500/90 text-white border-0 shadow-sm pl-1.5">
          <CheckCircle2 className="h-3 w-3" aria-hidden="true" />
          {counts.Accepted} accepted
        </Badge>
        <Badge variant="secondary" className="bg-linear-to-r from-rose-500/90 to-pink-500/90 text-white border-0 shadow-sm pl-1.5">
          <Ban className="h-3 w-3" aria-hidden="true" />
          {counts.Rejected} rejected
        </Badge>
        {counts.Modified > 0 && (
          <Badge variant="secondary" className="bg-linear-to-r from-sky-500/90 to-indigo-500/90 text-white border-0 shadow-sm pl-1.5">
            <PenLine className="h-3 w-3" aria-hidden="true" />
            {counts.Modified} modified
          </Badge>
        )}
      </div>

      {hasPending && (
        <>
          <Separator />
          <div className="flex gap-2 justify-center">
            <Button
              size="sm"
              variant="accept"
              className="gap-1 flex-1"
              onClick={handleAcceptAll}
              disabled={isDisabled}
            >
              <CheckCheck className="h-3.5 w-3.5" aria-hidden="true" />
              Accept All ({counts.Pending})
            </Button>
            <Button
              size="sm"
              variant="destructive"
              className="gap-1 flex-1"
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
