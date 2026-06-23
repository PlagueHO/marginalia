import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { Trash2 } from "lucide-react";

interface DeleteConfirmationDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onConfirm: () => void;
  isDeleting: boolean;
  documentTitle: string;
}

export function DeleteConfirmationDialog({
  open,
  onOpenChange,
  onConfirm,
  isDeleting,
  documentTitle,
}: DeleteConfirmationDialogProps) {
  return (
    <Dialog open={open} onOpenChange={isDeleting ? undefined : onOpenChange}>
      <DialogContent
        className="max-h-[calc(100vh-2rem)] overflow-y-auto sm:max-w-md"
        aria-describedby="delete-dialog-description"
        onInteractOutside={(e) => {
          if (isDeleting) e.preventDefault();
        }}
        onEscapeKeyDown={(e) => {
          if (isDeleting) e.preventDefault();
        }}
      >
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Trash2 className="h-5 w-5 text-destructive" aria-hidden="true" />
            Delete Manuscript
          </DialogTitle>
          <DialogDescription id="delete-dialog-description">
            Are you sure you want to delete &ldquo;{documentTitle}&rdquo;? This
            action cannot be undone and all suggestions will be permanently
            removed.
          </DialogDescription>
        </DialogHeader>

        <DialogFooter>
          <Button
            variant="outline"
            className="w-full min-h-11 sm:w-auto sm:min-h-8"
            onClick={() => onOpenChange(false)}
            disabled={isDeleting}
          >
            Cancel
          </Button>
          <Button
            variant="destructive"
            onClick={onConfirm}
            disabled={isDeleting}
            className="w-full min-h-11 gap-2 sm:w-auto sm:min-h-8"
          >
            {isDeleting ? (
              <Spinner />
            ) : (
              <Trash2 className="h-4 w-4" aria-hidden="true" />
            )}
            {isDeleting ? "Deleting…" : "Delete"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
