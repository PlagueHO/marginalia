import * as React from "react"

import { cn, CONTROL_HEIGHTS } from "@/lib/utils"

function Input({ className, type, ...props }: React.ComponentProps<"input">) {
  return (
    <input
      type={type}
      data-slot="input"
      className={cn(
        "w-full min-w-0 rounded-sm border border-input bg-transparent px-3 py-1 text-base shadow-xs transition-[color,box-shadow] outline-none selection:bg-primary selection:text-primary-foreground file:inline-flex file:h-full file:border-0 file:bg-transparent file:text-sm file:font-medium file:text-foreground placeholder:text-muted-foreground disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 pointer-coarse:min-h-11 md:text-sm dark:bg-input/30",
        CONTROL_HEIGHTS.default,
        "focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50",
        "aria-invalid:border-destructive aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40",
        type === "file" && "border-0 p-0 shadow-none file:mr-3 file:rounded-sm file:border file:border-input file:bg-background file:px-3 file:shadow-xs hover:file:bg-accent aria-invalid:file:border-destructive dark:bg-transparent",
        className
      )}
      {...props}
    />
  )
}

export { Input }
