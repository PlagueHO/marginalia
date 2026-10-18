import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export const CONTROL_HEIGHTS = {
  default: "h-11 sm:h-10",
  sm: "h-11 sm:h-8",
  lg: "h-11",
} as const;

export const headingText = "font-semibold tracking-tight text-foreground";

/** Secondary descriptive text styling. Combine with layout utilities as needed. */
export const mutedText = "text-sm text-muted-foreground";
