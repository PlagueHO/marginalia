import { useCallback, useState, useRef } from "react";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Upload, FileText, ClipboardPaste } from "lucide-react";
import { cn, mutedText } from "@/lib/utils";

interface DocumentUploaderProps {
  onFileUpload: (file: File, title?: string) => Promise<void>;
  onPaste: (content: string, filename?: string, title?: string) => Promise<void>;
  isLoading: boolean;
}

export function DocumentUploader({
  onFileUpload,
  onPaste,
  isLoading,
}: DocumentUploaderProps) {
  const [isDragOver, setIsDragOver] = useState(false);
  const [showPaste, setShowPaste] = useState(false);
  const [pasteContent, setPasteContent] = useState("");
  const [title, setTitle] = useState("");
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleDragOver = useCallback(
    (e: React.DragEvent) => {
      e.preventDefault();
      e.stopPropagation();
      setIsDragOver(true);
    },
    []
  );

  const handleDragLeave = useCallback(
    (e: React.DragEvent) => {
      e.preventDefault();
      e.stopPropagation();
      setIsDragOver(false);
    },
    []
  );

  const handleDrop = useCallback(
    async (e: React.DragEvent) => {
      e.preventDefault();
      e.stopPropagation();
      setIsDragOver(false);

      const file = e.dataTransfer.files[0];
      if (file) {
        await onFileUpload(file, title || undefined);
      }
    },
    [onFileUpload, title]
  );

  const handleFileSelect = useCallback(
    async (e: React.ChangeEvent<HTMLInputElement>) => {
      const file = e.target.files?.[0];
      if (file) {
        await onFileUpload(file, title || undefined);
      }
    },
    [onFileUpload, title]
  );

  const handlePasteSubmit = useCallback(async () => {
    if (pasteContent.trim()) {
      await onPaste(pasteContent.trim(), undefined, title || undefined);
      setPasteContent("");
      setShowPaste(false);
    }
  }, [pasteContent, onPaste, title]);

  return (
    <div className="flex flex-col items-center gap-6 w-full max-w-2xl mx-auto">
      <Input
        type="text"
        placeholder="Enter manuscript title (optional)"
        value={title}
        onChange={(e: React.ChangeEvent<HTMLInputElement>) => setTitle(e.target.value)}
        className="w-full"
        aria-label="Manuscript title"
        disabled={isLoading}
      />

      <Card
        className={`w-full border-2 border-dashed transition-colors cursor-pointer ${
          isDragOver
            ? "border-primary bg-primary/5"
            : "border-muted-foreground/25 hover:border-primary/50"
        }`}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
        onClick={() => fileInputRef.current?.click()}
        role="button"
        tabIndex={0}
        aria-label="Drop zone for document upload"
        onKeyDown={(e: React.KeyboardEvent) => {
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            fileInputRef.current?.click();
          }
        }}
      >
        <CardContent className="flex flex-col items-center justify-center py-12 gap-4">
          <div className="rounded-full bg-linear-to-br from-violet-600 to-indigo-600 p-4 shadow-md shadow-violet-900/30">
            <Upload
              className="h-8 w-8 text-white"
              aria-hidden="true"
            />
          </div>
          <div className="text-center">
            <p className="text-lg font-medium">
              Drop your manuscript here
            </p>
            <p className={cn(mutedText, "mt-1")}>
              or click to browse — supports Word documents (.docx)
            </p>
          </div>
          <input
            ref={fileInputRef}
            type="file"
            accept=".docx,.doc,.txt"
            className="hidden"
            onChange={handleFileSelect}
            aria-label="Upload document file"
            disabled={isLoading}
          />
          {isLoading && (
            <p className={cn(mutedText, "animate-pulse")}>
              Processing document…
            </p>
          )}
        </CardContent>
      </Card>

      <div className="flex items-center gap-4 w-full">
        <div className="flex-1 h-px bg-border" />
        <span className={mutedText}>or</span>
        <div className="flex-1 h-px bg-border" />
      </div>

      {showPaste ? (
        <Card className="w-full">
          <CardContent className="pt-6 flex flex-col gap-4">
            <div className="flex items-center gap-2">
              <FileText className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
              <span className="text-sm font-medium">Paste your text</span>
            </div>
            <Textarea
              placeholder="Paste your manuscript text here…"
              value={pasteContent}
              onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => setPasteContent(e.target.value)}
              rows={8}
              className="resize-y"
              aria-label="Manuscript text input"
              disabled={isLoading}
            />
            <div className="flex gap-2 justify-end">
              <Button
                variant="outline"
                onClick={() => {
                  setShowPaste(false);
                  setPasteContent("");
                }}
                disabled={isLoading}
              >
                Cancel
              </Button>
              <Button
                onClick={handlePasteSubmit}
                disabled={!pasteContent.trim() || isLoading}
              >
                {isLoading ? "Processing…" : "Load Text"}
              </Button>
            </div>
          </CardContent>
        </Card>
      ) : (
        <Button
          onClick={() => setShowPaste(true)}
          className="gap-2"
          disabled={isLoading}
        >
          <ClipboardPaste className="h-4 w-4" aria-hidden="true" />
          Paste text instead
        </Button>
      )}
    </div>
  );
}
