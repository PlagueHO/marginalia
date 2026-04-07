import { BrowserRouter, Routes, Route } from "react-router-dom";
import { TooltipProvider } from "@/components/ui/tooltip";
import { Toaster } from "@/components/ui/sonner";
import { EditorPage } from "@/pages/EditorPage";
import { HomePage } from "@/pages/HomePage";
import { AccessCodeDialog } from "@/components/AccessCodeDialog";
import { useAccessCode } from "@/hooks/useAccessCode";
import { Loader2 } from "lucide-react";

function App() {
  const { accessCodeRequired, isVerified, isLoading, error, submitCode } = useAccessCode();

  if (isLoading) {
    return (
      <div className="flex h-screen items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" aria-label="Loading" />
      </div>
    );
  }

  if (accessCodeRequired && !isVerified) {
    return (
      <AccessCodeDialog
        open={true}
        onSubmit={submitCode}
        isLoading={false}
        error={error}
      />
    );
  }

  return (
    <BrowserRouter>
      <TooltipProvider delayDuration={300}>
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/new" element={<EditorPage />} />
          <Route path="/editor/:documentId" element={<EditorPage />} />
        </Routes>
        <Toaster richColors position="bottom-right" />
      </TooltipProvider>
    </BrowserRouter>
  );
}

export default App;
