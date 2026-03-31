# shadcn/ui + Vite Setup

> Confidence: high
> Source: Marginalia frontend implementation (2025-07-22)

## Pattern

When installing shadcn/ui components in a Vite + React (non-Next.js) project:

### Known Issues

1. **Path alias resolution**: The shadcn CLI may create a literal `@/` directory instead of resolving the path alias to `src/`. After running `pnpm dlx shadcn@latest add ...`, verify files landed in `src/components/ui/`. If they're in `@/components/ui/`, move them manually.

1. **Sonner (toast replacement)**: The `toast` component is deprecated — use `sonner` instead. The generated `sonner.tsx` may:
   - Import from itself (circular reference)
   - Use `next-themes` which isn't available in Vite projects

   **Fix**: Replace the import to use the `sonner` npm package directly and remove the `useTheme()` call.

1. **react-refresh lint warnings**: shadcn components like `badge.tsx`, `button.tsx`, and `tabs.tsx` export both components and helper functions (e.g., `badgeVariants`, `buttonVariants`), triggering `react-refresh/only-export-components`. These are safe to ignore — they're generated code.

### Recommended Install Command

```bash
pnpm dlx shadcn@latest add button card input textarea badge dialog dropdown-menu separator tabs sonner tooltip scroll-area alert sheet label switch --yes
```

### Post-Install Checklist

- [ ] Verify components are in `src/components/ui/`
- [ ] Fix `sonner.tsx` imports (remove next-themes, fix circular import)
- [ ] Wrap app root with `<TooltipProvider>` if using tooltips
- [ ] Add `<Toaster>` from sonner to app root

## When to Apply

- Any new Vite + React + TypeScript project using shadcn/ui
- Especially when the project uses path aliases (`@/` → `./src/*`)
