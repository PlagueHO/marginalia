import { Moon, Sun } from "lucide-react";
import { useTheme } from "@/hooks/useTheme";
import { cn } from "@/lib/utils";

interface ThemeToggleProps {
  className?: string;
}

export function ThemeToggle({ className }: ThemeToggleProps) {
  const { resolvedTheme, toggleTheme } = useTheme();
  const isDark = resolvedTheme === "dark";

  return (
    <button
      onClick={toggleTheme}
      aria-label={isDark ? "Switch to light mode" : "Switch to dark mode"}
      className={cn(
        "inline-flex min-h-11 items-center rounded-full border border-border bg-muted/40 p-1 gap-1 cursor-pointer",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background",
        className
      )}
    >
      <span
        aria-hidden="true"
        className={cn(
          "flex h-8 w-8 items-center justify-center rounded-full transition-all duration-200",
          !isDark
            ? "bg-background shadow-sm text-amber-500"
            : "text-muted-foreground"
        )}
      >
        <Sun className="h-3.5 w-3.5" />
      </span>

      <span
        aria-hidden="true"
        className={cn(
          "flex h-8 w-8 items-center justify-center rounded-full transition-all duration-200",
          isDark
            ? "bg-background shadow-sm text-violet-400 dark:bg-zinc-800"
            : "text-muted-foreground"
        )}
      >
        <Moon className="h-3.5 w-3.5" />
      </span>
    </button>
  );
}
