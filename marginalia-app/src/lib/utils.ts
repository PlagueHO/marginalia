import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

/** Shared brand gradient for heading text. Combine with size utilities (e.g. text-lg, text-3xl). */
export const gradientText =
  "font-bold tracking-tight bg-linear-to-r from-violet-400 via-indigo-400 to-purple-400 bg-clip-text text-transparent";

/** Secondary descriptive text styling. Combine with layout utilities as needed. */
export const mutedText = "text-sm text-muted-foreground";
