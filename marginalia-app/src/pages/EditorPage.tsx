import { useCallback, useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useDocument } from "@/hooks/useDocument";
import { useSuggestions } from "@/hooks/useSuggestions";
import { useAnalysis } from "@/hooks/useAnalysis";
import { usePageTitle } from "@/hooks/usePageTitle";
import { AppHeader } from "@/components/AppHeader";
import { MainLayout } from "@/components/MainLayout";
import { DocumentUploader } from "@/components/DocumentUploader";
import { DocumentHeader } from "@/components/DocumentHeader";
import { DocumentEditor } from "@/components/DocumentEditor";
import { SuggestionPanel } from "@/components/SuggestionPanel";
import { AnalysisDialog } from "@/components/AnalysisDialog";
import { ReplaceAnalysisConfirmationDialog } from "@/components/ReplaceAnalysisConfirmationDialog";
import { DeleteConfirmationDialog } from "@/components/DeleteConfirmationDialog";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { AlertCircle } from "lucide-react";
import type { Document, SuggestionStatus, ApiError } from "@/types";
import { toast } from "sonner";
import { mergeAcceptedSuggestionsToParagraphs } from "@/lib/suggestionUtils";
import * as documentService from "@/services/documentService";
import * as suggestionService from "@/services/suggestionService";

function isApiError(err: unknown): err is ApiError {
  return typeof err === "object" && err !== null && "statusCode" in err && "message" in err;
}

export function EditorPage() {
  const { documentId } = useParams<{ documentId: string }>();
  const navigate = useNavigate();
  const doc = useDocument();
  const suggestions = useSuggestions();
  const analysis = useAnalysis();
  const [isAnalysisOpen, setIsAnalysisOpen] = useState(false);
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const [pendingAnalysisParams, setPendingAnalysisParams] = useState<{
    guidance?: string;
    tone?: string;
  } | null>(null);
  const [reanalyzeParagraphId, setReanalyzeParagraphId] = useState<string | null>(null);
  const [isParagraphAnalyzing, setIsParagraphAnalyzing] = useState(false);

  const normalizedError = doc.error?.toLowerCase() ?? "";
  const isManuscriptNotFound =
    Boolean(documentId) &&
    Boolean(doc.error) &&
    (normalizedError.includes("404") || normalizedError.includes("not found"));

  let pageTitle: string;
  if (isManuscriptNotFound) {
    pageTitle = "Manuscript not found";
  } else if (documentId && !doc.document && doc.isLoading) {
    pageTitle = "Loading Manuscript";
  } else if (documentId && !doc.document) {
    pageTitle = "Manuscript";
  } else if (!documentId && doc.isLoading) {
    pageTitle = "Loading Manuscript";
  } else if (!documentId && !doc.document) {
    pageTitle = "New Manuscript";
  } else {
    const documentName =
      doc.document?.title?.trim() ||
      doc.document?.filename?.trim() ||
      "Untitled Manuscript";
    pageTitle = documentId
      ? `Manuscript: ${documentName}`
      : `Editing: ${documentName}`;
  }

  usePageTitle(pageTitle);

  useEffect(() => {
    if (documentId && !doc.document) {
      doc.loadDocument(documentId).then((loaded) => {
        if (loaded?.suggestions) {
          suggestions.setSuggestions(loaded.suggestions);
        }
        if (loaded?.paragraphs) {
          suggestions.setParagraphs(loaded.paragraphs);
        }
      }).catch(() => {
        toast.error("Failed to load document");
      });
    } else if (!documentId && doc.document) {
      doc.clearDocument();
      suggestions.setSuggestions([]);
      suggestions.setParagraphs([]);
    }
  }, [documentId]); // eslint-disable-line react-hooks/exhaustive-deps

  const handleFileUpload = useCallback(
    async (file: File, title?: string) => {
      try {
        const response = await doc.uploadFile(file, title);
        suggestions.setSuggestions(response.document.suggestions);
        suggestions.setParagraphs(response.document.paragraphs);
        toast.success("Document loaded successfully");
        navigate(`/editor/${response.document.id}`, { replace: true });
      } catch {
        toast.error("Failed to upload document");
      }
    },
    [doc, suggestions, navigate]
  );

  const handlePaste = useCallback(
    async (content: string, filename?: string, title?: string) => {
      try {
        const response = await doc.pasteContent(content, filename, title);
        suggestions.setSuggestions(response.document.suggestions);
        suggestions.setParagraphs(response.document.paragraphs);
        toast.success("Text loaded successfully");
        navigate(`/editor/${response.document.id}`, { replace: true });
      } catch {
        toast.error("Failed to process text");
      }
    },
    [doc, suggestions, navigate]
  );

  const runAnalysis = useCallback(
    async (guidance?: string, tone?: string) => {
      if (!doc.document) return;

      const userGuidance = [guidance, tone ? `Tone: ${tone}` : ""]
        .filter(Boolean)
        .join(". ") || undefined;

      // Single-paragraph reanalysis
      if (reanalyzeParagraphId) {
        const paragraphId = reanalyzeParagraphId;
        setIsParagraphAnalyzing(true);
        try {
          const newSuggestions = await documentService.analyzeParagraph(
            doc.document.id,
            paragraphId,
            userGuidance ? { userGuidance } : undefined
          );

          // Refresh the full list so paragraph-level status transitions from
          // the backend are reflected before selecting the new suggestion.
          const refreshedSuggestions = await suggestionService.getSuggestions(doc.document.id);
          suggestions.setSuggestions(refreshedSuggestions);

          // Keep the newly generated suggestion selected so its card remains expanded.
          suggestions.setActiveSuggestion(newSuggestions[0]?.id ?? null);
          toast.success(
            newSuggestions.length > 0
              ? `Generated ${newSuggestions.length} new suggestion${newSuggestions.length > 1 ? "s" : ""}`
              : "No suggestions generated for this paragraph"
          );
        } catch (err) {
          toast.error(
            isApiError(err) && err.statusCode === 422
              ? `Content blocked by safety filter: ${err.message}`
              : "Paragraph analysis failed — check backend connection"
          );
        } finally {
          setIsParagraphAnalyzing(false);
          setReanalyzeParagraphId(null);
        }
        return;
      }

      // Full-document analysis
      try {
        // Snapshot the current paragraphs and suggestions before the API call
        // so we can merge only accepted suggestions on the frontend.
        const preParagraphs = doc.document.paragraphs;
        const preSuggestions = suggestions.suggestions;

        const analysisResult = await analysis.analyze({
          documentId: doc.document.id,
          userGuidance,
        });

        // Merge only accepted/modified suggestions into the paragraph text.
        // This is computed on the frontend so we know exactly which suggestions
        // are applied — pending and rejected suggestions are left out.
        const mergedParagraphs = mergeAcceptedSuggestionsToParagraphs(
          preParagraphs,
          preSuggestions,
        );

        doc.updateDocument({
          ...doc.document,
          paragraphs: mergedParagraphs,
          suggestions: analysisResult,
          status: "Analyzed",
        });
        suggestions.setSuggestions(analysisResult);
        suggestions.setParagraphs(mergedParagraphs);

        toast.success(`Found ${analysisResult.length} suggestions`);
      } catch (err) {
        toast.error(
          isApiError(err) && err.statusCode === 422
            ? `Content blocked by safety filter: ${err.message}`
            : "Analysis failed — check backend connection"
        );
      }
    },
    [doc, analysis, suggestions, reanalyzeParagraphId]
  );

  const handleAnalyze = useCallback(
    (guidance?: string, tone?: string) => {
      if (!doc.document) return;

      // Single-paragraph reanalysis skips the confirmation flow
      if (reanalyzeParagraphId) {
        void runAnalysis(guidance, tone);
        return;
      }

      // Build a current view of the document using live suggestion state,
      // because doc.document may not reflect suggestions added after first analysis.
      const currentDocument: Document = {
        ...doc.document,
        suggestions: suggestions.suggestions,
        status: suggestions.suggestions.length > 0 ? "Analyzed" as const : doc.document.status,
      };

      // Check if confirmation is needed for re-analysis
      const result = analysis.initiateAnalysis(currentDocument);
      if (result === "confirm") {
        // Store the analysis params and close the analysis dialog.
        // The confirmation dialog will open via analysis.showConfirmReplaceDialog.
        setPendingAnalysisParams({ guidance, tone });
        setIsAnalysisOpen(false);
        return;
      }

      // No confirmation needed — proceed with analysis directly
      void runAnalysis(guidance, tone);
    },
    [doc, suggestions.suggestions, analysis, runAnalysis, reanalyzeParagraphId]
  );

  const handleConfirmReplaceAnalysis = useCallback(async () => {
    if (!doc.document || !pendingAnalysisParams) return;

    // Close the confirmation dialog and reopen the analysis dialog for progress
    analysis.confirmReplaceAndAnalyze();
    setIsAnalysisOpen(true);

    const { guidance, tone } = pendingAnalysisParams;
    setPendingAnalysisParams(null);

    await runAnalysis(guidance, tone);
  }, [doc, pendingAnalysisParams, analysis, runAnalysis]);

  const handleCancelReplaceAnalysis = useCallback(
    (open: boolean) => {
      if (!open) {
        analysis.cancelReplaceAnalysis();
        setPendingAnalysisParams(null);
      }
    },
    [analysis]
  );

  const handleReanalyzeParagraph = useCallback(
    (paragraphId: string) => {
      setReanalyzeParagraphId(paragraphId);
      setIsAnalysisOpen(true);
    },
    []
  );

  const handleSuggestionStatusChange = useCallback(
    async (id: string, status: SuggestionStatus, modifiedText?: string) => {
      if (!doc.document) return;
      try {
        await suggestions.updateStatus(
          doc.document.id,
          id,
          status,
          modifiedText
        );
      } catch {
        toast.error("Failed to update suggestion");
      }
    },
    [doc.document, suggestions]
  );

  const handleAcceptAll = useCallback(async () => {
    if (!doc.document) return;
    try {
      await suggestions.acceptAll(doc.document.id);
      toast.success("All suggestions accepted");
    } catch {
      toast.error("Failed to accept all suggestions");
    }
  }, [doc.document, suggestions]);

  const handleRejectAll = useCallback(async () => {
    if (!doc.document) return;
    try {
      await suggestions.rejectAll(doc.document.id);
      toast.success("All suggestions rejected");
    } catch {
      toast.error("Failed to reject all suggestions");
    }
  }, [doc.document, suggestions]);

  const handleRename = useCallback(
    async (title: string) => {
      try {
        await doc.renameDocument(title);
        toast.success("Title updated");
      } catch {
        toast.error("Failed to update title");
      }
    },
    [doc]
  );

  const handleDelete = useCallback(async () => {
    try {
      await doc.deleteDocument();
      suggestions.setSuggestions([]);
      suggestions.setParagraphs([]);
      setIsDeleteOpen(false);
      toast.success("Document deleted");
      navigate("/");
    } catch {
      toast.error("Failed to delete document");
    }
  }, [doc, suggestions, navigate]);

  const error = doc.error ?? analysis.error;
  const isAnalyzing = analysis.isAnalyzing || isParagraphAnalyzing;
  const analysisProgress = analysis.isAnalyzing
    ? analysis.progress
    : isParagraphAnalyzing
      ? "Analyzing paragraph…"
      : "";

  const editorContent = doc.document ? (
    <div className="flex flex-col h-full">
      <DocumentHeader
        document={doc.document}
        suggestions={suggestions.suggestions}
        onRename={handleRename}
        onAnalyze={() => setIsAnalysisOpen(true)}
        onDelete={() => setIsDeleteOpen(true)}
      />
      <DocumentEditor
        document={doc.document}
        suggestions={suggestions.filteredSuggestions}
        activeSuggestionId={suggestions.activeSuggestionId}
        hoveredSuggestionId={suggestions.hoveredSuggestionId}
        suggestionNumbers={suggestions.suggestionNumbers}
        onSuggestionClick={suggestions.setActiveSuggestion}
        onSuggestionHover={suggestions.setHoveredSuggestion}
      />
    </div>
  ) : (
    <DocumentUploader
      onFileUpload={handleFileUpload}
      onPaste={handlePaste}
      isLoading={doc.isLoading}
    />
  );

  const panelContent = doc.document ? (
    <SuggestionPanel
      suggestions={suggestions.suggestions}
      filteredSuggestions={suggestions.filteredSuggestions}
      filter={suggestions.filter}
      activeSuggestionId={suggestions.activeSuggestionId}
      hoveredSuggestionId={suggestions.hoveredSuggestionId}
      suggestionNumbers={suggestions.suggestionNumbers}
      paragraphs={doc.document?.paragraphs}
      counts={suggestions.counts}
      onFilterChange={suggestions.setFilter}
      onStatusChange={handleSuggestionStatusChange}
      onSuggestionClick={suggestions.setActiveSuggestion}
      onSuggestionHover={suggestions.setHoveredSuggestion}
      onAcceptAll={handleAcceptAll}
      onRejectAll={handleRejectAll}
      onReanalyze={handleReanalyzeParagraph}
      isUnanalyzed={doc.document.status === "Draft"}
      onAnalyze={() => setIsAnalysisOpen(true)}
    />
  ) : null;

  return (
    <div className="flex flex-col h-screen">
      <AppHeader />

      {error && (
        <Alert variant="destructive" className="rounded-none">
          <AlertCircle className="h-4 w-4" />
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      <MainLayout
        editor={editorContent}
        panel={panelContent}
        hasDocument={!!doc.document}
      />

      {doc.document && (
        <AnalysisDialog
          open={isAnalysisOpen}
          onOpenChange={(open) => {
            setIsAnalysisOpen(open);
            if (!open) setReanalyzeParagraphId(null);
          }}
          isAnalyzing={isAnalyzing}
          progress={analysisProgress}
          onAnalyze={handleAnalyze}
          paragraphMode={!!reanalyzeParagraphId}
        />
      )}

      {doc.document && (
        <DeleteConfirmationDialog
          open={isDeleteOpen}
          onOpenChange={setIsDeleteOpen}
          onConfirm={() => void handleDelete()}
          isDeleting={doc.isLoading}
          documentTitle={doc.document.title || doc.document.filename}
        />
      )}

      {doc.document && (
        <ReplaceAnalysisConfirmationDialog
          open={analysis.showConfirmReplaceDialog}
          onOpenChange={handleCancelReplaceAnalysis}
          acceptedSuggestions={analysis.acceptedSuggestions}
          pendingCount={analysis.pendingCount}
          rejectedCount={analysis.rejectedCount}
          onConfirm={() => void handleConfirmReplaceAnalysis()}
        />
      )}
    </div>
  );
}