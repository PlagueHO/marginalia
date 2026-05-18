import { Moon, Sun } from "lucide-react";
import { Switch as SwitchPrimitive } from "radix-ui";
import { useTheme } from "@/hooks/useTheme";
import { cn } from "@/lib/utils";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";

interface ThemeToggleProps {
  className?: string;
}

export function ThemeToggle({ className }: ThemeToggleProps) {
  const { resolvedTheme, toggleTheme } = useTheme();
  const isDark = resolvedTheme === "dark";

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <SwitchPrimitive.Root
          checked={isDark}
          onCheckedChange={toggleTheme}
          aria-label={isDark ? "Switch to light mode" : "Switch to dark mode"}
          className={cn(
            "inline-flex h-[26px] w-[48px] shrink-0 cursor-pointer items-center rounded-full border-2 border-transparent transition-colors",
            "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background",
            "disabled:cursor-not-allowed disabled:opacity-50",
            "data-[state=unchecked]:bg-muted data-[state=checked]:bg-muted dark:data-[state=checked]:bg-zinc-700",
            className
          )}
        >
          <SwitchPrimitive.Thumb
            className={cn(
              "pointer-events-none flex h-[22px] w-[22px] items-center justify-center rounded-full shadow-sm ring-0 transition-transform",
              "data-[state=checked]:translate-x-[22px] data-[state=unchecked]:translate-x-0",
              "bg-background dark:bg-zinc-900"
            )}
          >
            {isDark ? (
              <Moon className="h-3 w-3 text-violet-400" aria-hidden="true" />
            ) : (
              <Sun className="h-3 w-3 text-amber-500" aria-hidden="true" />
            )}
          </SwitchPrimitive.Thumb>
        </SwitchPrimitive.Root>
      </TooltipTrigger>
      <TooltipContent>{isDark ? "Light mode" : "Dark mode"}</TooltipContent>
    </Tooltip>
  );
}
