import { Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";

interface SpinnerProps {
  className?: string;
  size?: "sm" | "md";
}

export function Spinner({ className, size = "sm" }: SpinnerProps) {
  const sizeClass = size === "sm" ? "h-4 w-4" : "h-5 w-5";
  return (
    <Loader2
      className={cn(sizeClass, "animate-spin", className)}
      aria-hidden="true"
    />
  );
}
