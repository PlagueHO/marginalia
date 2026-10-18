import { useEffect } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useDocuments } from "@/hooks/useDocuments";
import { usePageTitle } from "@/hooks/usePageTitle";
import { cn, headingText, mutedText } from "@/lib/utils";
import { Spinner } from "@/components/ui/spinner";
import { AppHeader } from "@/components/AppHeader";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { PlusCircle, FileText, AlertCircle } from "lucide-react";
import { Alert, AlertDescription } from "@/components/ui/alert";

export function HomePage() {
  const { documents, isLoading, error, loadDocuments } = useDocuments();
  const navigate = useNavigate();
  const pageTitle = isLoading ? "Loading Manuscripts" : "Home";

  usePageTitle(pageTitle);

  useEffect(() => {
    void loadDocuments();
  }, [loadDocuments]);

  return (
    <div className="flex flex-col h-screen">
      <AppHeader />

      <main id="main-content" tabIndex={-1} className="flex-1 overflow-auto">
        <div className="mx-auto flex w-full max-w-4xl flex-col gap-6 px-4 py-8 sm:px-6 sm:py-10">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="space-y-2">
            <h1 className={cn(headingText, "text-3xl")}>
              Your Manuscripts
            </h1>
            <p className="text-muted-foreground">
              Review past work or start something new
            </p>
          </div>

          <Button
            size="lg"
            className="w-full gap-2 sm:w-auto"
            onClick={() => navigate("/new")}
          >
            <PlusCircle className="h-5 w-5" aria-hidden="true" />
            New Manuscript
          </Button>
          </div>

          {error && (
            <Alert variant="destructive" className="w-full">
              <AlertCircle className="h-4 w-4" aria-hidden="true" />
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          {isLoading ? (
            <div role="status" className="flex items-center gap-2 py-12 text-muted-foreground">
              <Spinner size="md" />
              <span>Loading manuscripts…</span>
            </div>
          ) : documents.length === 0 && !error ? (
            <Card className="w-full">
              <CardContent className="flex flex-col items-center justify-center gap-4 py-16">
                <FileText className="h-12 w-12 text-muted-foreground/50" aria-hidden="true" />
                <div className="text-center space-y-1">
                  <p className="text-lg font-medium">No manuscripts yet</p>
                  <p className={mutedText}>
                    Create your first one to get started
                  </p>
                </div>
              </CardContent>
            </Card>
          ) : (
            <div className="grid w-full gap-3">
              {documents.map((doc) => (
                <Link
                  key={doc.id}
                  to={`/editor/${doc.id}`}
                  className="block rounded-xl border bg-card text-card-foreground transition-colors hover:bg-accent/50"
                  aria-label={`Open manuscript ${doc.title}`}
                >
                  <CardContent className="flex flex-col gap-3 px-4 py-4 sm:flex-row sm:items-center sm:justify-between sm:px-6">
                    <div className="flex min-w-0 items-center gap-4">
                      <FileText className="h-5 w-5 shrink-0 text-muted-foreground" aria-hidden="true" />
                      <div className="min-w-0">
                        <p className="truncate font-medium">{doc.title}</p>
                        <p className={mutedText}>
                          {new Date(doc.updatedAt).toLocaleDateString(undefined, {
                            year: "numeric",
                            month: "short",
                            day: "numeric",
                          })}
                        </p>
                      </div>
                    </div>
                    <div className="flex shrink-0 items-center justify-between gap-3 sm:justify-end">
                      {doc.suggestionCount > 0 && (
                        <span className={mutedText}>
                          {doc.suggestionCount} suggestion{doc.suggestionCount !== 1 ? "s" : ""}
                        </span>
                      )}
                      <Badge variant={doc.status === "Analyzed" ? "default" : "secondary"}>
                        {doc.status}
                      </Badge>
                    </div>
                  </CardContent>
                </Link>
              ))}
            </div>
          )}
        </div>
      </main>
    </div>
  );
}
