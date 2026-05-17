import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { MemoryRouter } from "react-router-dom";
import { TooltipProvider } from "@/components/ui/tooltip";
import { AppHeader } from "@/components/AppHeader";

const mockNavigate = vi.fn();

vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useLocation: () => ({ pathname: "/" }),
  };
});

vi.mock("@/hooks/useTheme", () => ({
  useTheme: () => ({
    theme: "light",
    toggleTheme: vi.fn(),
  }),
}));

describe("AppHeader", () => {
  it("renders anonymous user trigger and dropdown content", async () => {
    const user = userEvent.setup();

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
  });

  it("navigates to settings when Settings is clicked", async () => {
    const user = userEvent.setup();

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
});
