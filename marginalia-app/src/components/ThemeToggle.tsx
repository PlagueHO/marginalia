import { Moon, Sun } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useTheme } from "@/hooks/useTheme";
import { cn } from "@/lib/utils";

interface ThemeToggleProps {
  className?: string;
}

export function ThemeToggle({ className }: ThemeToggleProps) {
  const { resolvedTheme, toggleTheme } = useTheme();
  const isDark = resolvedTheme === "dark";

  return (
    <Button
      variant="ghost"
      onClick={toggleTheme}
      aria-label={isDark ? "Switch to light mode" : "Switch to dark mode"}
      className={cn(
        "rounded-full border border-border bg-muted/40 p-1 gap-1 cursor-pointer",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background",
        className
      )}
    >
      <span
        aria-hidden="true"
        className={cn(
          "flex size-7 items-center justify-center rounded-full transition-all duration-200",
          !isDark
            ? "bg-background shadow-sm text-primary"
            : "text-muted-foreground"
        )}
      >
        <Sun className="size-3.5" />
      </span>

      <span
        aria-hidden="true"
        className={cn(
          "flex size-7 items-center justify-center rounded-full transition-all duration-200",
          isDark
            ? "bg-background shadow-sm text-primary"
            : "text-muted-foreground"
        )}
      >
        <Moon className="size-3.5" />
      </span>
    </Button>
  );
}
