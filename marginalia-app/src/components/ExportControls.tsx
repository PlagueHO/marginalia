import { useCallback, useState } from "react";
import { Button } from "@/components/ui/button";
import { Download, Loader2 } from "lucide-react";
import * as documentService from "@/services/documentService";

interface ExportControlsProps {
  documentId: string;
  filename: string;
}

export function ExportControls({
  documentId,
  filename,
}: ExportControlsProps) {
  const [isExporting, setIsExporting] = useState(false);

  const handleExport = useCallback(async () => {
    setIsExporting(true);
    try {
      const blob = await documentService.exportDocument(documentId);
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = filename.replace(/\.[^.]+$/, "") + "_revised.docx";
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    } catch {
      // Error is handled by the caller via toast
    } finally {
      setIsExporting(false);
    }
  }, [documentId, filename]);

  return (
    <Button
      variant="default"
      size="sm"
      className="gap-2"
      onClick={handleExport}
      disabled={isExporting}
    >
      {isExporting ? (
        <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
      ) : (
        <Download className="h-4 w-4" aria-hidden="true" />
      )}
      {isExporting ? "Exporting…" : "Export Word"}
    </Button>
  );
}
