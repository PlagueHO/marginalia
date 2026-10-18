import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { MemoryRouter } from "react-router-dom";
import { TooltipProvider } from "@/components/ui/tooltip";
import { AppHeader } from "@/components/AppHeader";

const mockNavigate = vi.fn();
let mockPathname = "/";

vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useLocation: () => ({ pathname: mockPathname }),
  };
});

vi.mock("@/hooks/useTheme", () => ({
  useTheme: () => ({
    resolvedTheme: "light",
    toggleTheme: vi.fn(),
  }),
}));

describe("AppHeader", () => {
  beforeEach(() => {
    mockPathname = "/";
    mockNavigate.mockReset();
  });

  it("renders anonymous user trigger and dropdown content", async () => {
    const user = userEvent.setup({ pointerEventsCheck: 0 });

    render(
      <MemoryRouter>
        <TooltipProvider>
          <AppHeader />
        </TooltipProvider>
      </MemoryRouter>
    );

    expect(screen.getByRole("button", { name: /user menu/i })).toHaveTextContent("Anonymous");

    await user.click(screen.getByRole("button", { name: /user menu/i }));

    expect(screen.getByText("Anonymous Mode")).toBeInTheDocument();
    expect(screen.getByText("Entra ID SSO is not configured.")).toBeInTheDocument();
    expect(screen.getByRole("menuitem", { name: "Settings" })).toBeInTheDocument();
    expect(screen.getByText("Disabled")).toBeInTheDocument();
  }, 15000);

  it("navigates to settings when Settings is clicked", async () => {
    const user = userEvent.setup({ pointerEventsCheck: 0 });

    render(
      <MemoryRouter>
        <TooltipProvider>
          <AppHeader />
        </TooltipProvider>
      </MemoryRouter>
    );

    await user.click(screen.getByRole("button", { name: /user menu/i }));
    await user.click(screen.getByRole("menuitem", { name: "Settings" }));

    expect(mockNavigate).toHaveBeenCalledWith("/settings");
  });

  it("opens mobile navigation sheet and navigates to settings", async () => {
    const user = userEvent.setup({ pointerEventsCheck: 0 });

    render(
      <MemoryRouter>
        <TooltipProvider>
          <AppHeader />
        </TooltipProvider>
      </MemoryRouter>
    );

    await user.click(screen.getByRole("button", { name: /open navigation menu/i }));
    const settings = screen.getByRole("link", { name: /^settings$/i });
    expect(settings).toHaveAttribute("href", "/settings");
    await user.click(settings);

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("uses current-page native links and keeps branding out of the heading hierarchy", () => {
    mockPathname = "/settings";
    render(<MemoryRouter><TooltipProvider><AppHeader /></TooltipProvider></MemoryRouter>);
    expect(screen.getByRole("link", { name: "Settings" })).toHaveAttribute("aria-current", "page");
    expect(screen.getByRole("link", { name: "Marginalia" })).toHaveAttribute("href", "/");
    expect(screen.queryByRole("heading", { level: 1 })).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: /skip to main content/i })).toHaveAttribute("href", "#main-content");
  });
});
