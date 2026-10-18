import { useState, useRef } from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Lock } from "lucide-react";
import { Spinner } from "@/components/ui/spinner";

interface AccessCodeDialogProps {
  open: boolean;
  onSubmit: (code: string) => Promise<void>;
  isLoading: boolean;
  error: string | null;
}

export function AccessCodeDialog({
  open,
  onSubmit,
  isLoading,
  error,
}: AccessCodeDialogProps) {
  const [code, setCode] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const displayedError = validationError || error;

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (isLoading) return;
    if (code.trim()) {
      setValidationError(null);
      await onSubmit(code.trim());
    } else {
      setValidationError("Enter an access code to continue.");
      inputRef.current?.focus();
    }
  }

  return (
    <Dialog open={open} onOpenChange={() => { /* non-dismissible */ }}>
      <DialogContent
        className="max-h-[calc(100vh-2rem)] overflow-y-auto sm:max-w-md"
        aria-describedby="access-code-description"
        showCloseButton={false}
        onPointerDownOutside={(e) => e.preventDefault()}
        onEscapeKeyDown={(e) => e.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Lock className="h-5 w-5" aria-hidden="true" />
            Access Code Required
          </DialogTitle>
          <DialogDescription id="access-code-description">
            This application is protected. Enter the access code to continue.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit}>
          <div className="flex flex-col gap-4 py-4">
            <div className="flex flex-col gap-2">
              <Label htmlFor="access-code-input">Access Code <span aria-hidden="true">*</span></Label>
              <Input
                ref={inputRef}
                id="access-code-input"
                type="password"
                value={code}
                onChange={(e) => {
                  setCode(e.target.value);
                  setValidationError(null);
                }}
                placeholder="Enter access code"
                disabled={isLoading}
                autoFocus
                aria-required="true"
                aria-invalid={displayedError ? true : undefined}
                aria-describedby={displayedError ? "access-code-error" : undefined}
                data-testid="access-code-input"
              />
              {displayedError && (
                <p id="access-code-error" role="alert" className="text-sm text-destructive" data-testid="access-code-error">
                  {displayedError}
                </p>
              )}
            </div>
          </div>

          <DialogFooter>
            <Button
              type="submit"
              disabled={isLoading}
              className="w-full gap-2 sm:w-auto"
            >
              {isLoading && (
                <Spinner />
              )}
              Continue
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
