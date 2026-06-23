import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { MainLayout } from "@/components/MainLayout";

describe("MainLayout", () => {
  it("shows mobile view toggles when a document is loaded", async () => {
    const user = userEvent.setup();

    render(
      <MainLayout
        hasDocument={true}
        editor={<div>Editor content</div>}
        panel={<div>Suggestion content</div>}
      />
    );

    const editorButton = screen.getByRole("button", { name: "Editor" });
    const suggestionsButton = screen.getByRole("button", { name: "Suggestions" });

    expect(editorButton).toHaveAttribute("aria-pressed", "true");
    expect(suggestionsButton).toHaveAttribute("aria-pressed", "false");

    await user.click(suggestionsButton);

    expect(editorButton).toHaveAttribute("aria-pressed", "false");
    expect(suggestionsButton).toHaveAttribute("aria-pressed", "true");
  });

  it("hides mobile view toggles when there is no document", () => {
    render(
      <MainLayout
        hasDocument={false}
        editor={<div>Uploader content</div>}
        panel={null}
      />
    );

    expect(screen.queryByRole("button", { name: "Editor" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Suggestions" })).not.toBeInTheDocument();
  });

  it("resets to editor view after document is cleared", async () => {
    const user = userEvent.setup();
    const { rerender } = render(
      <MainLayout
        hasDocument={true}
        editor={<div>Editor content</div>}
        panel={<div>Suggestion content</div>}
      />
    );

    await user.click(screen.getByRole("button", { name: "Suggestions" }));

    rerender(
      <MainLayout
        hasDocument={false}
        editor={<div>Uploader content</div>}
        panel={null}
      />
    );

    rerender(
      <MainLayout
        hasDocument={true}
        editor={<div>Editor content</div>}
        panel={<div>Suggestion content</div>}
      />
    );

    expect(screen.getByRole("button", { name: "Editor" })).toHaveAttribute(
      "aria-pressed",
      "true"
    );
    expect(screen.getByRole("button", { name: "Suggestions" })).toHaveAttribute(
      "aria-pressed",
      "false"
    );
  });
});
