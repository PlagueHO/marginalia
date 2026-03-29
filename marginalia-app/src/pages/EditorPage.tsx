import { useCallback, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useDocument } from "@/hooks/useDocument";
import { useSuggestions } from "@/hooks/useSuggestions";
import { useAnalysis } from "@/hooks/useAnalysis";
import { useLlmConfig } from "@/hooks/useLlmConfig";
import { AppHeader } from "@/components/AppHeader";
import { MainLayout } from "@/components/MainLayout";
import { DocumentUploader } from "@/components/DocumentUploader";
import { DocumentHeader } from "@/components/DocumentHeader";
import { DocumentEditor } from "@/components/DocumentEditor";
import { SuggestionPanel } from "@/components/SuggestionPanel";
import { AnalysisControls } from "@/components/AnalysisControls";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { AlertCircle } from "lucide-react";
import type { SuggestionStatus } from "@/types";
import { toast } from "sonner";

export function EditorPage() {
  const { documentId } = useParams<{ documentId: string }>();
  const navigate = useNavigate();
  const doc = useDocument();
  const suggestions = useSuggestions();
  const analysis = useAnalysis();
  const llmConfig = useLlmConfig();

  useEffect(() => {
    if (documentId && !doc.document) {
      doc.loadDocument(documentId).then((loaded) => {
        if (loaded?.suggestions) {
          suggestions.setSuggestions(loaded.suggestions);
        }
      }).catch(() => {
        toast.error("Failed to load document");
      });
    }
  }, [documentId]); // eslint-disable-line react-hooks/exhaustive-deps

  const handleFileUpload = useCallback(
    async (file: File, title?: string) => {
      try {
        const response = await doc.uploadFile(file, title);
        suggestions.setSuggestions(response.document.suggestions);
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
        toast.success("Text loaded successfully");
        navigate(`/editor/${response.document.id}`, { replace: true });
      } catch {
        toast.error("Failed to process text");
      }
    },
    [doc, suggestions, navigate]
  );

  const handleAnalyze = useCallback(
    async (guidance?: string, tone?: string) => {
      if (!doc.document) return;
      try {
        const result = await analysis.analyze({
          documentId: doc.document.id,
          content: doc.document.content,
          userGuidance: [guidance, tone ? `Tone: ${tone}` : ""]
            .filter(Boolean)
            .join(". ") || undefined,
        });
        suggestions.setSuggestions(result);
        toast.success(`Found ${result.length} suggestions`);
      } catch {
        toast.error("Analysis failed — check your model configuration");
      }
    },
    [doc.document, analysis, suggestions]
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

  const handleNewSession = useCallback(() => {
    doc.clearDocument();
    suggestions.setSuggestions([]);
    navigate("/");
  }, [doc, suggestions, navigate]);

  const handleCheckHealth = useCallback(async () => {
    await llmConfig.checkHealth();
  }, [llmConfig]);

  const error = doc.error ?? analysis.error;

  const editorContent = doc.document ? (
    <div className="flex flex-col h-full">
      <DocumentHeader document={doc.document} />
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
      counts={suggestions.counts}
      onFilterChange={suggestions.setFilter}
      onStatusChange={handleSuggestionStatusChange}
      onSuggestionClick={suggestions.setActiveSuggestion}
      onSuggestionHover={suggestions.setHoveredSuggestion}
      onAcceptAll={handleAcceptAll}
      onRejectAll={handleRejectAll}
    />
  ) : null;

  const controlsContent = doc.document ? (
    <AnalysisControls
      documentId={doc.document.id}
      isAnalyzing={analysis.isAnalyzing}
      progress={analysis.progress}
      onAnalyze={handleAnalyze}
    />
  ) : null;

  return (
    <div className="flex flex-col h-screen">
      <AppHeader
        documentId={doc.document?.id}
        filename={doc.document?.filename}
        llmConfig={llmConfig.config}
        isConfigLoading={llmConfig.isLoading}
        isCheckingHealth={llmConfig.isCheckingHealth}
        healthResult={llmConfig.healthResult}
        onNewSession={handleNewSession}
        onCheckHealth={handleCheckHealth}
      />

      {error && (
        <Alert variant="destructive" className="rounded-none">
          <AlertCircle className="h-4 w-4" />
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      <MainLayout
        editor={editorContent}
        panel={panelContent}
        controls={controlsContent}
        hasDocument={!!doc.document}
      />
    </div>
  );
}
