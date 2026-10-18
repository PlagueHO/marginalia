import type { LucideIcon } from "lucide-react";
import { BookOpen } from "lucide-react";
import { cn } from "@/lib/utils";

interface AppLoadingScreenProps {
  icon?: LucideIcon;
  className?: string;
  spinnerClassName?: string;
  iconClassName?: string;
}

export function AppLoadingScreen({
  icon: Icon = BookOpen,
  className,
  spinnerClassName,
  iconClassName,
}: AppLoadingScreenProps) {
  return (
    <div role="status" className={cn("flex h-screen flex-col items-center justify-center gap-6", className)}>
      <span className="text-sm text-muted-foreground">Loading Marginalia...</span>
      <div className="relative flex h-24 w-24 items-center justify-center">
        <div
          className={cn(
            "h-24 w-24 animate-spin rounded-full border-4 border-muted border-t-primary",
            spinnerClassName
          )}
        />
        <Icon
          className={cn("absolute h-9 w-9 text-primary", iconClassName)}
          aria-hidden="true"
        />
      </div>
    </div>
  );
}
