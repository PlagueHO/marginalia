import { BrowserRouter, Routes, Route } from "react-router-dom";
import { TooltipProvider } from "@/components/ui/tooltip";
import { Toaster } from "@/components/ui/sonner";
import { EditorPage } from "@/pages/EditorPage";
import { HomePage } from "@/pages/HomePage";

function App() {
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
