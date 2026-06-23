import {
  useCallback,
  useEffect,
  useRef,
  useState,
  type ReactNode,
  type RefObject,
} from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

interface MainLayoutProps {
  editor: ReactNode;
  panel: ReactNode;
  hasDocument: boolean;
}

const MIN_PCT = 20;
const MAX_PCT = 80;
const DEFAULT_PCT = 65;

interface LoadedDocumentLayoutProps {
  editor: ReactNode;
  panel: ReactNode;
  splitPct: number;
  containerRef: RefObject<HTMLDivElement | null>;
  onResizeStart: (event: React.MouseEvent) => void;
}

function LoadedDocumentLayout({
  editor,
  panel,
  splitPct,
  containerRef,
  onResizeStart,
}: LoadedDocumentLayoutProps) {
  const [mobileView, setMobileView] = useState<"editor" | "suggestions">("editor");
  const hasPanel = panel != null;

  return (
    <main id="main-content" className="flex-1 flex flex-col overflow-hidden">
      {hasPanel && (
        <div className="border-b px-3 py-2 lg:hidden">
          <div className="grid grid-cols-2 gap-2">
            <Button
              variant={mobileView === "editor" ? "secondary" : "ghost"}
              className="min-h-11"
              onClick={() => setMobileView("editor")}
              aria-pressed={mobileView === "editor"}
            >
              Editor
            </Button>
            <Button
              variant={mobileView === "suggestions" ? "secondary" : "ghost"}
              className="min-h-11"
              onClick={() => setMobileView("suggestions")}
              aria-pressed={mobileView === "suggestions"}
            >
              Suggestions
            </Button>
          </div>
        </div>
      )}

      <div
        ref={containerRef}
        className="flex flex-1 flex-col overflow-hidden lg:flex-row"
      >
        <div
          className={cn(
            "flex flex-1 min-w-0 flex-col overflow-hidden",
            hasPanel && mobileView !== "editor" && "hidden lg:flex"
          )}
          style={hasPanel ? { width: `${splitPct}%` } : undefined}
        >
          <div className="flex-1 overflow-hidden">{editor}</div>
        </div>

        {/* Drag handle */}
        {hasPanel && (
          <div
            role="separator"
            aria-orientation="vertical"
            aria-label="Resize panels"
            className="group hidden w-1.5 shrink-0 cursor-col-resize items-stretch lg:flex"
            onMouseDown={onResizeStart}
          >
            <div className="mx-auto w-px bg-border/50 transition-colors group-hover:bg-primary/50 group-active:bg-primary" />
          </div>
        )}

        {hasPanel && (
          <div
            className={cn(
              "h-full shrink-0 overflow-y-auto border-t lg:h-auto lg:border-t-0",
              mobileView !== "suggestions" && "hidden lg:block"
            )}
            style={{ width: `${100 - splitPct}%` }}
          >
            {panel}
          </div>
        )}
      </div>
    </main>
  );
}

export function MainLayout({
  editor,
  panel,
  hasDocument,
}: MainLayoutProps) {
  const [splitPct, setSplitPct] = useState(DEFAULT_PCT);
  const isDragging = useRef(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const handleMouseDown = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    isDragging.current = true;
    document.body.style.cursor = "col-resize";
    document.body.style.userSelect = "none";
  }, []);

  useEffect(() => {
    const handleMouseMove = (e: MouseEvent) => {
      if (!isDragging.current || !containerRef.current) return;
      const rect = containerRef.current.getBoundingClientRect();
      const pct = ((e.clientX - rect.left) / rect.width) * 100;
      setSplitPct(Math.min(MAX_PCT, Math.max(MIN_PCT, pct)));
    };

    const handleMouseUp = () => {
      if (!isDragging.current) return;
      isDragging.current = false;
      document.body.style.cursor = "";
      document.body.style.userSelect = "";
    };

    document.addEventListener("mousemove", handleMouseMove);
    document.addEventListener("mouseup", handleMouseUp);
    return () => {
      document.removeEventListener("mousemove", handleMouseMove);
      document.removeEventListener("mouseup", handleMouseUp);
    };
  }, []);

  if (!hasDocument) {
    return (
      <main
        id="main-content"
        className="flex-1 flex items-center justify-center p-4 sm:p-8"
      >
        {editor}
      </main>
    );
  }

  return (
    <LoadedDocumentLayout
      editor={editor}
      panel={panel}
      splitPct={splitPct}
      containerRef={containerRef}
      onResizeStart={handleMouseDown}
    />
  );
}
