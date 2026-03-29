import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useDocuments } from "@/hooks/useDocuments";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { PlusCircle, FileText, Loader2, AlertCircle, BookOpen } from "lucide-react";
import { Alert, AlertDescription } from "@/components/ui/alert";

export function HomePage() {
  const { documents, isLoading, error, loadDocuments } = useDocuments();
  const navigate = useNavigate();

  useEffect(() => {
    void loadDocuments();
  }, [loadDocuments]);

  return (
    <div className="flex flex-col h-screen">
      <header className="flex items-center justify-between px-4 py-2.5 border-b border-border/50 bg-linear-to-r from-background via-background to-muted/30 dark:from-zinc-950 dark:via-zinc-900/80 dark:to-zinc-800/40 backdrop-blur-md supports-backdrop-filter:bg-background/60 sticky top-0 z-50 shadow-sm">
        <div className="flex items-center gap-3">
          <BookOpen className="h-5 w-5 text-violet-400" aria-hidden="true" />
          <h1 className="text-lg font-bold tracking-tight bg-linear-to-r from-violet-400 via-indigo-400 to-purple-400 bg-clip-text text-transparent">Marginalia</h1>
        </div>
      </header>

      <div className="flex-1 overflow-auto">
        <div className="flex flex-col items-center gap-8 w-full max-w-4xl mx-auto px-4 py-12">
      <div className="text-center space-y-2">
        <h2 className="text-3xl font-bold tracking-tight bg-linear-to-r from-violet-400 via-indigo-400 to-purple-400 bg-clip-text text-transparent">
          Your Manuscripts
        </h2>
        <p className="text-muted-foreground">
          Review past work or start something new
        </p>
      </div>

      <Button
        size="lg"
        className="gap-2"
        onClick={() => navigate("/new")}
      >
        <PlusCircle className="h-5 w-5" aria-hidden="true" />
        New Manuscript
      </Button>

      {error && (
        <Alert variant="destructive" className="w-full">
          <AlertCircle className="h-4 w-4" />
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {isLoading ? (
        <div className="flex items-center gap-2 text-muted-foreground py-12">
          <Loader2 className="h-5 w-5 animate-spin" aria-hidden="true" />
          <span>Loading manuscripts…</span>
        </div>
      ) : documents.length === 0 && !error ? (
        <Card className="w-full">
          <CardContent className="flex flex-col items-center justify-center py-16 gap-4">
            <FileText className="h-12 w-12 text-muted-foreground/50" aria-hidden="true" />
            <div className="text-center space-y-1">
              <p className="text-lg font-medium">No manuscripts yet</p>
              <p className="text-sm text-muted-foreground">
                Create your first one to get started
              </p>
            </div>
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-3 w-full">
          {documents.map((doc) => (
            <Card
              key={doc.id}
              className="cursor-pointer transition-colors hover:bg-accent/50"
              onClick={() => navigate(`/editor/${doc.id}`)}
              role="link"
              tabIndex={0}
              onKeyDown={(e: React.KeyboardEvent) => {
                if (e.key === "Enter" || e.key === " ") {
                  e.preventDefault();
                  navigate(`/editor/${doc.id}`);
                }
              }}
            >
              <CardContent className="flex items-center justify-between py-4 px-6">
                <div className="flex items-center gap-4 min-w-0">
                  <FileText className="h-5 w-5 text-muted-foreground shrink-0" aria-hidden="true" />
                  <div className="min-w-0">
                    <p className="font-medium truncate">{doc.title}</p>
                    <p className="text-sm text-muted-foreground">
                      {new Date(doc.updatedAt).toLocaleDateString(undefined, {
                        year: "numeric",
                        month: "short",
                        day: "numeric",
                      })}
                    </p>
                  </div>
                </div>
                <div className="flex items-center gap-3 shrink-0">
                  {doc.suggestionCount > 0 && (
                    <span className="text-sm text-muted-foreground">
                      {doc.suggestionCount} suggestion{doc.suggestionCount !== 1 ? "s" : ""}
                    </span>
                  )}
                  <Badge variant={doc.status === "Analyzed" ? "default" : "secondary"}>
                    {doc.status}
                  </Badge>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
        </div>
      </div>
    </div>
  );
}
