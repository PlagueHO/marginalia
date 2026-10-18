import { useCallback, useState, useRef, useEffect } from "react";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
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
  const [fileError, setFileError] = useState<string | null>(null);
  const [pasteError, setPasteError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const pasteInputRef = useRef<HTMLTextAreaElement>(null);
  const pasteButtonRef = useRef<HTMLButtonElement>(null);
  const restorePasteFocusRef = useRef(false);

  useEffect(() => {
    if (showPaste) {
      pasteInputRef.current?.focus();
    } else if (restorePasteFocusRef.current) {
      pasteButtonRef.current?.focus();
      restorePasteFocusRef.current = false;
    }
  }, [showPaste]);

  const uploadFile = useCallback(async (file: File) => {
    if (isLoading) return;
    const error = !file.name.toLowerCase().endsWith(".docx")
      ? "Choose a Word document (.docx)."
      : file.size > 50 * 1024 * 1024
        ? "Choose a document smaller than or equal to 50 MB."
        : null;
    setFileError(error);
    if (error) {
      fileInputRef.current?.focus();
      return;
    }
    try {
      await onFileUpload(file, title || undefined);
    } catch (err) {
      setFileError(err instanceof Error ? err.message : "Upload failed. Please try again.");
    }
  }, [isLoading, onFileUpload, title]);

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
        await uploadFile(file);
      }
    },
    [uploadFile]
  );

  const handleFileSelect = useCallback(
    async (e: React.ChangeEvent<HTMLInputElement>) => {
      const file = e.target.files?.[0];
      if (file) {
        await uploadFile(file);
      }
      e.target.value = "";
    },
    [uploadFile]
  );

  const handlePasteSubmit = useCallback(async () => {
    if (isLoading) return;
    if (!pasteContent.trim()) {
      setPasteError("Paste manuscript text before loading it.");
      pasteInputRef.current?.focus();
      return;
    }
    setPasteError(null);
    try {
      await onPaste(pasteContent.trim(), undefined, title || undefined);
      setPasteContent("");
      restorePasteFocusRef.current = true;
      setShowPaste(false);
    } catch (err) {
      setPasteError(err instanceof Error ? err.message : "Text could not be loaded. Please try again.");
    }
  }, [isLoading, pasteContent, onPaste, title]);

  return (
    <div className="mx-auto flex w-full max-w-2xl flex-col gap-6 py-4 sm:py-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-semibold tracking-tight">New Manuscript</h1>
        <p className="text-muted-foreground">Upload a Word document or paste your manuscript text.</p>
      </div>
      <div className="space-y-2">
      <Label htmlFor="manuscript-title">Manuscript title (optional)</Label>
      <Input
        id="manuscript-title"
        type="text"
        placeholder="Enter manuscript title (optional)"
        value={title}
        onChange={(e: React.ChangeEvent<HTMLInputElement>) => setTitle(e.target.value)}
        className="w-full"
        disabled={isLoading}
      />
      </div>

      <Card
        className={`w-full border transition-colors ${
          isDragOver
            ? "border-primary bg-primary/10 shadow-sm"
            : "border-input bg-muted/30"
        }`}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
      >
        <CardContent className="flex flex-col items-center justify-center gap-4 py-8 sm:py-12">
          <div className="rounded-xl bg-primary/10 p-3">
            <Upload
              className="h-7 w-7 text-primary"
              aria-hidden="true"
            />
          </div>
          <div className="text-center">
            <p className="text-lg font-medium">
              Drop your manuscript here
            </p>
            <p id="upload-help" className={cn(mutedText, "mt-1")}>
              Supports Word documents (.docx), up to 50 MB.
            </p>
          </div>
          <Button variant="outline" onClick={() => fileInputRef.current?.click()}
            aria-describedby="upload-help" disabled={isLoading}>
            Browse manuscript
          </Button>
          {isLoading && (
            <p role="status" className={mutedText}>
              Processing document…
            </p>
          )}
        </CardContent>
      </Card>
      <input ref={fileInputRef} type="file" accept=".docx"
        className="sr-only focus:not-sr-only" tabIndex={-1} onChange={handleFileSelect}
        aria-label="Upload document file" aria-describedby={fileError ? "upload-help upload-error" : "upload-help"}
        aria-invalid={fileError ? true : undefined} disabled={isLoading} />
      {fileError && <p id="upload-error" role="alert" className="text-sm text-destructive">{fileError}</p>}

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
              <Label htmlFor="manuscript-text">Paste your text <span aria-hidden="true">*</span></Label>
            </div>
            <Textarea
              ref={pasteInputRef}
              id="manuscript-text"
              placeholder="Paste your manuscript text here…"
              value={pasteContent}
              onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => {
                setPasteContent(e.target.value);
                setPasteError(null);
              }}
              rows={8}
              className="resize-y"
              aria-describedby={pasteError ? "paste-error" : undefined}
              aria-invalid={pasteError ? true : undefined}
              disabled={isLoading}
              aria-required="true"
            />
            {pasteError && <p id="paste-error" role="alert" className="text-sm text-destructive">{pasteError}</p>}
            <div className="flex flex-col justify-end gap-2 sm:flex-row">
              <Button
                variant="outline"
                onClick={() => {
                  restorePasteFocusRef.current = true;
                  setShowPaste(false);
                  setPasteContent("");
                  setPasteError(null);
                }}
                disabled={isLoading}
                className="w-full sm:w-auto"
              >
                Cancel
              </Button>
              <Button
                onClick={handlePasteSubmit}
                disabled={isLoading}
                className="w-full sm:w-auto"
              >
                {isLoading ? "Processing…" : "Load Text"}
              </Button>
            </div>
          </CardContent>
        </Card>
      ) : (
        <Button
          ref={pasteButtonRef}
          onClick={() => setShowPaste(true)}
          className="w-full gap-2 sm:w-auto"
          disabled={isLoading}
        >
          <ClipboardPaste className="h-4 w-4" aria-hidden="true" />
          Paste text instead
        </Button>
      )}
    </div>
  );
}
