import type { ReactNode } from "react";

interface MainLayoutProps {
  editor: ReactNode;
  panel: ReactNode;
  controls: ReactNode;
  hasDocument: boolean;
}

export function MainLayout({
  editor,
  panel,
  controls,
  hasDocument,
}: MainLayoutProps) {
  if (!hasDocument) {
    return (
      <main className="flex-1 flex items-center justify-center p-8">
        {editor}
      </main>
    );
  }

  return (
    <main className="flex-1 flex flex-col lg:flex-row overflow-hidden">
      <div className="flex-1 flex flex-col min-w-0 lg:w-[65%]">
        <div className="flex-1 overflow-hidden">{editor}</div>
        {controls}
      </div>
      <div className="h-72 lg:h-auto lg:w-[35%] overflow-y-auto">
        {panel}
      </div>
    </main>
  );
}
