import { useMemo } from "react";
import { Link, useNavigate, useLocation } from "react-router-dom";
import { Button, buttonVariants } from "@/components/ui/button";
import { cn, headingText } from "@/lib/utils";
import {
  BookOpen,
  CircleUser,
  Home,
  LogIn,
  Menu,
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
import {
  Sheet,
  SheetClose,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from "@/components/ui/sheet";

export function AppHeader() {
  const navigate = useNavigate();
  const location = useLocation();

  const isHome = location.pathname === "/";
  const isNew = location.pathname === "/new";
  const isSettings = location.pathname === "/settings";

  const navItems = useMemo(
    () => [
      {
        label: "Manuscripts",
        icon: Home,
        path: "/",
        isActive: isHome,
      },
      {
        label: "New",
        icon: PlusCircle,
        path: "/new",
        isActive: isNew,
      },
      {
        label: "Settings",
        icon: Settings,
        path: "/settings",
        isActive: isSettings,
      },
    ],
    [isHome, isNew, isSettings]
  );

  const tabBase =
    cn(buttonVariants({ variant: "ghost" }), "gap-1.5 rounded-md px-3 cursor-pointer");
  const tabActive =
    "bg-accent text-foreground";
  const tabInactive =
    "text-muted-foreground hover:text-foreground hover:bg-accent/50";

  return (
    <header className="sticky top-0 z-50 flex items-center justify-between border-b bg-background px-2 py-2 sm:px-6 sm:py-3">
      <a
        href="#main-content"
        className="sr-only rounded-md focus:not-sr-only focus:absolute focus:left-3 focus:top-3 focus:z-50 focus:bg-background focus:px-3 focus:py-2 focus:text-sm focus:shadow"
      >
        Skip to main content
      </a>

      {/* Left: Brand + Navigation */}
      <div className="flex min-w-0 items-center gap-1 sm:gap-2">
        <Sheet>
          <SheetTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              aria-label="Open navigation menu"
              className="md:hidden"
            >
              <Menu className="size-5" aria-hidden="true" />
            </Button>
          </SheetTrigger>
          <SheetContent
            side="left"
            className="w-[min(85vw,20rem)] p-0"
            aria-describedby="mobile-navigation-description"
          >
            <SheetHeader className="border-b">
              <SheetTitle className={cn(headingText, "text-left text-base")}>
                Navigation
              </SheetTitle>
              <SheetDescription id="mobile-navigation-description">
                Open manuscripts, start a new draft, or manage settings.
              </SheetDescription>
            </SheetHeader>
            <nav aria-label="Mobile navigation" className="flex flex-col gap-2 p-4">
              {navItems.map(({ label, path, icon: Icon, isActive: active }) => (
                <SheetClose key={path} asChild>
                  <Button
                    asChild
                    variant={active ? "secondary" : "ghost"}
                    className="justify-start gap-2"
                  >
                    <Link to={path} aria-current={active ? "page" : undefined}>
                      <Icon className="size-4" aria-hidden="true" />
                      <span>{label}</span>
                    </Link>
                  </Button>
                </SheetClose>
              ))}
            </nav>
          </SheetContent>
        </Sheet>

        <Link
          to="/"
          className="flex items-center gap-2.5 hover:opacity-80 transition-opacity mr-2"
          aria-label="Marginalia"
        >
          <BookOpen className="h-5 w-5 text-primary" aria-hidden="true" />
          <span className={cn(headingText, "text-lg hidden min-[380px]:block")}>
            Marginalia
          </span>
        </Link>

        <Separator orientation="vertical" className="mx-1 hidden h-6 md:block" />

        <nav className="hidden items-center gap-0.5 md:flex" aria-label="Primary navigation">
          {navItems.map(({ label, path, icon: Icon, isActive: active }) => (
            <Link key={path} to={path} aria-current={active ? "page" : undefined}
              className={cn(tabBase, active ? tabActive : tabInactive)}>
              <Icon className="h-4 w-4" aria-hidden="true" />
              <span>{label}</span>
            </Link>
          ))}
        </nav>
      </div>

      {/* Right: Theme toggle + User Menu */}
      <div className="flex items-center gap-1 sm:gap-2">
        <ThemeToggle />

        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              aria-label="Anonymous user menu"
              className="gap-2 px-2 sm:px-3"
              title="Entra ID SSO is not enabled. Running in anonymous single-user mode."
            >
              <CircleUser className="size-5" aria-hidden="true" />
              <span className="hidden text-sm sm:inline">Anonymous</span>
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
