import { Badge } from "@/components/ui/badge";
import { FileText, Hash, Calendar } from "lucide-react";
import type { Document } from "@/types";

interface DocumentHeaderProps {
  document: Document;
}

export function DocumentHeader({ document }: DocumentHeaderProps) {
  return (
    <div className="flex items-center gap-3 px-4 py-2.5 border-b bg-linear-to-r from-muted/20 via-muted/10 to-transparent">
      <FileText className="h-4 w-4 text-muted-foreground shrink-0" aria-hidden="true" />
      <h2 className="text-sm font-medium truncate">{document.filename}</h2>
      <Badge variant="secondary" className="gap-1 shrink-0">
        <Hash className="h-3 w-3" aria-hidden="true" />
        {document.source}
      </Badge>
      <span className="text-xs text-muted-foreground ml-auto flex items-center gap-1 shrink-0">
        <Calendar className="h-3 w-3" aria-hidden="true" />
        {document.content.length.toLocaleString()} characters
      </span>
    </div>
  );
}
