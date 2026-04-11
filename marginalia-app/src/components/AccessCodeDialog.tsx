import { useState } from "react";
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

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (code.trim()) {
      await onSubmit(code.trim());
    }
  }

  return (
    <Dialog open={open} onOpenChange={() => { /* non-dismissible */ }}>
      <DialogContent
        className="sm:max-w-md"
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
              <Label htmlFor="access-code-input">Access Code</Label>
              <Input
                id="access-code-input"
                type="password"
                value={code}
                onChange={(e) => setCode(e.target.value)}
                placeholder="Enter access code"
                disabled={isLoading}
                autoFocus
                data-testid="access-code-input"
              />
              {error && (
                <p className="text-sm text-destructive" data-testid="access-code-error">
                  {error}
                </p>
              )}
            </div>
          </div>

          <DialogFooter>
            <Button
              type="submit"
              disabled={isLoading || !code.trim()}
              className="gap-2"
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
