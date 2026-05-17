import { BrowserRouter, Routes, Route } from "react-router-dom";
import { TooltipProvider } from "@/components/ui/tooltip";
import { Toaster } from "@/components/ui/sonner";
import { EditorPage } from "@/pages/EditorPage";
import { HomePage } from "@/pages/HomePage";
import { SettingsPage } from "@/pages/SettingsPage";
import { AccessCodeDialog } from "@/components/AccessCodeDialog";
import { AppLoadingScreen } from "@/components/AppLoadingScreen";
import { useAccessCode } from "@/hooks/useAccessCode";

function App() {
  const { accessCodeRequired, isVerified, isLoading, error, submitCode } = useAccessCode();

  if (isLoading) {
    return <AppLoadingScreen />;
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
          <Route path="/settings" element={<SettingsPage />} />
        </Routes>
        <Toaster richColors position="bottom-right" />
      </TooltipProvider>
    </BrowserRouter>
  );
}

export default App;
