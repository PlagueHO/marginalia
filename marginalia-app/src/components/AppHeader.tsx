import { Link, useNavigate, useLocation } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { cn, gradientText } from "@/lib/utils";
import {
  BookOpen,
  CircleUser,
  Home,
  LogIn,
  PlusCircle,
  Settings,
} from "lucide-react";
import { Separator } from "@/components/ui/separator";
import { ThemeToggle } from "@/components/ThemeToggle";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

export function AppHeader() {
  const navigate = useNavigate();
  const location = useLocation();

  const isHome = location.pathname === "/";
  const isNew = location.pathname === "/new";

  const tabBase =
    "inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium rounded-md transition-colors cursor-pointer";
  const tabActive =
    "bg-accent text-foreground";
  const tabInactive =
    "text-muted-foreground hover:text-foreground hover:bg-accent/50";

  return (
    <header className="flex items-center justify-between px-4 py-2.5 border-b border-border/50 bg-linear-to-r from-background via-background to-muted/30 dark:from-zinc-950 dark:via-zinc-900/80 dark:to-zinc-800/40 backdrop-blur-md supports-backdrop-filter:bg-background/60 sticky top-0 z-50 shadow-sm">
      {/* Left: Brand + Navigation */}
      <div className="flex items-center gap-1">
        <Link
          to="/"
          className="flex items-center gap-2.5 hover:opacity-80 transition-opacity mr-2"
        >
          <BookOpen className="h-5 w-5 text-violet-400" aria-hidden="true" />
          <h1 className={cn(gradientText, "text-lg hidden sm:block")}>
            Marginalia
          </h1>
        </Link>

        <Separator orientation="vertical" className="h-6 mx-1" />

        <nav className="flex items-center gap-0.5" role="tablist" aria-label="Navigation">
          <button
            role="tab"
            aria-selected={isHome}
            className={`${tabBase} ${isHome ? tabActive : tabInactive}`}
            onClick={() => navigate("/")}
          >
            <Home className="h-4 w-4" aria-hidden="true" />
            <span className="hidden md:inline">Manuscripts</span>
          </button>

          <button
            role="tab"
            aria-selected={isNew}
            className={`${tabBase} ${isNew ? tabActive : tabInactive}`}
            onClick={() => navigate("/new")}
          >
            <PlusCircle className="h-4 w-4" aria-hidden="true" />
            <span className="hidden md:inline">New</span>
          </button>
        </nav>
      </div>

      {/* Right: Theme toggle + User Menu */}
      <div className="flex items-center gap-2">
        <ThemeToggle />

        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              size="sm"
              aria-label="User menu"
              className="gap-2 opacity-60"
              title="Entra ID SSO is not enabled. Running in anonymous single-user mode."
            >
              <CircleUser className="size-5" />
              <span className="text-sm">Anonymous</span>
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-56">
            <DropdownMenuLabel className="font-normal">
              <div className="flex flex-col space-y-1">
                <p className="text-sm font-medium leading-none">Anonymous Mode</p>
                <p className="text-xs leading-none text-muted-foreground">
                  Entra ID SSO is not configured.
                </p>
              </div>
            </DropdownMenuLabel>
            <DropdownMenuSeparator />
            <DropdownMenuGroup>
              <DropdownMenuItem
                onClick={() => void navigate("/settings")}
                className="gap-2"
              >
                <Settings className="size-4" aria-hidden="true" />
                Settings
              </DropdownMenuItem>
            </DropdownMenuGroup>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              disabled
              className="gap-2"
              title="Enable Entra ID SSO to sign in. See QUICKSTART-AZURE.md for setup instructions."
            >
              <LogIn className="size-4" aria-hidden="true" />
              Sign in
              <span className="ml-auto text-xs text-muted-foreground">Disabled</span>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </header>
  );
}
