import { TooltipProvider } from "@/components/ui/tooltip";
import { Toaster } from "@/components/ui/sonner";
import { EditorPage } from "@/pages/EditorPage";

function App() {
  return (
    <TooltipProvider delayDuration={300}>
      <EditorPage />
      <Toaster richColors position="bottom-right" />
    </TooltipProvider>
  );
}

export default App;
