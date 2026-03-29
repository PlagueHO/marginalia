import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Loader2, CheckCircle2, XCircle, ShieldCheck } from "lucide-react";
import type { LlmConfig, LlmHealthResult } from "@/types";

interface LlmConfigDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  config: LlmConfig;
  isLoading: boolean;
  isCheckingHealth: boolean;
  healthResult: LlmHealthResult | null;
  onCheckHealth: () => Promise<void>;
}

export function LlmConfigDialog({
  open,
  onOpenChange,
  config,
  isLoading,
  isCheckingHealth,
  healthResult,
  onCheckHealth,
}: LlmConfigDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md" aria-describedby="llm-config-description">
        <DialogHeader>
          <DialogTitle>Model Configuration</DialogTitle>
          <DialogDescription id="llm-config-description">
            Azure AI Foundry endpoint configuration. Authentication is managed
            by the backend service using Entra ID.
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-4 py-4">
          <div className="flex flex-col gap-2">
            <Label>Endpoint URL</Label>
            <div
              data-testid="endpoint-display"
              className="rounded-md border bg-muted px-3 py-2 font-mono text-sm text-muted-foreground break-all"
            >
              {config.endpoint ?? <span className="italic">Not configured</span>}
            </div>
          </div>

          <div className="flex flex-col gap-2">
            <Label>Model Name</Label>
            <div
              data-testid="model-name-display"
              className="rounded-md border bg-muted px-3 py-2 text-sm text-muted-foreground"
            >
              {config.modelName ?? <span className="italic">Not configured</span>}
            </div>
          </div>

          <div className="flex flex-col gap-2">
            <Label>Authentication</Label>
            <div>
              <Badge variant="outline" className="gap-1.5" data-testid="entra-id-badge">
                <ShieldCheck className="h-3 w-3" aria-hidden="true" />
                Entra ID (Default Azure Credential)
              </Badge>
            </div>
          </div>

          <div className="flex flex-col gap-2">
            <Label>Connection Status</Label>
            {isCheckingHealth ? (
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
                Checking connection...
              </div>
            ) : healthResult ? (
              <div
                className={`flex items-start gap-2 text-sm ${healthResult.healthy ? "text-green-600" : "text-destructive"}`}
                data-testid="health-status"
              >
                {healthResult.healthy ? (
                  <CheckCircle2 className="h-4 w-4 mt-0.5 shrink-0" aria-hidden="true" />
                ) : (
                  <XCircle className="h-4 w-4 mt-0.5 shrink-0" aria-hidden="true" />
                )}
                <span>
                  <span className="font-medium">
                    {healthResult.healthy ? "Connected" : "Disconnected"}
                  </span>
                  {" — "}
                  {healthResult.message}
                </span>
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">Not checked</p>
            )}
          </div>
        </div>

        <DialogFooter>
          <Button
            onClick={onCheckHealth}
            disabled={isLoading || isCheckingHealth}
            className="gap-2"
          >
            {isCheckingHealth && (
              <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
            )}
            Check Connection
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
