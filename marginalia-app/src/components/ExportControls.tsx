import { useCallback, useState } from "react";
import { Button } from "@/components/ui/button";
import { Download } from "lucide-react";
import { Spinner } from "@/components/ui/spinner";
import { toast } from "sonner";
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
      toast.error("Export failed. Please try again.");
    } finally {
      setIsExporting(false);
    }
  }, [documentId, filename]);

  return (
    <Button
      size="sm"
      variant="outline"
      className="min-w-24 gap-2 justify-center"
      onClick={handleExport}
      disabled={isExporting}
    >
      {isExporting ? (
        <Spinner />
      ) : (
        <Download className="h-4 w-4" aria-hidden="true" />
      )}
      {isExporting ? "Exporting…" : "Export"}
    </Button>
  );
}
