import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { axe } from "jest-axe";
import { DocumentEditor } from "@/components/DocumentEditor";
import { TooltipProvider } from "@/components/ui/tooltip";
import type { Document, Suggestion } from "@/types";

const manuscript: Document = {
  id: "doc-1", userId: "_anonymous", filename: "sample.docx", title: "Sample",
  source: "Local", status: "Draft", createdAt: "", updatedAt: "", suggestions: [],
  paragraphs: [
    { id: "p-1", text: "First paragraph.\nA deliberate line break.\n\nAnd a blank line." },
    { id: "p-2", text: `${"A long sentence with  internal spaces. ".repeat(30)}End.` },
    { id: "p-3", text: "" },
  ],
};
const suggestion: Suggestion = {
  id: "s-1", documentId: "doc-1", paragraphId: "p-1", status: "Pending",
  rationale: "Clarify the opening.", proposedChange: "A revised opening.",
};

function renderEditor(suggestions: Suggestion[] = []) {
  const onSuggestionClick = vi.fn();
  const view = render(
    <TooltipProvider>
      <DocumentEditor document={manuscript} suggestions={suggestions}
        activeSuggestionId={null} hoveredSuggestionId={null}
        suggestionNumbers={new Map([["s-1", 1], ["s-2", 2]])}
        onSuggestionClick={onSuggestionClick} onSuggestionHover={vi.fn()} />
    </TooltipProvider>
  );
  return { ...view, onSuggestionClick };
}

describe("DocumentEditor", () => {
  it("preserves paragraph text, ordering, existing layout and manuscript typography", () => {
    renderEditor();
    const document = screen.getByRole("document");
    expect([...document.querySelectorAll("p")].map((p) => p.textContent))
      .toEqual(manuscript.paragraphs.map((p) => p.text));
    expect(document).toHaveClass("px-4", "ml-8", "max-w-none", "prose-sm", "sm:prose-base", "leading-relaxed", "font-serif", "space-y-4");
    expect(document.querySelectorAll("br")).toHaveLength(0);
  });

  it("exposes independently keyboard-operable suggestion markers and highlighted paragraphs", async () => {
    const user = userEvent.setup();
    const { onSuggestionClick, container } = renderEditor([suggestion]);
    const marker = screen.getByRole("button", { name: /suggestion 1, pending/i });
    marker.focus();
    await user.keyboard("{Enter}");
    expect(onSuggestionClick).toHaveBeenLastCalledWith("s-1");
    const paragraph = screen.getByRole("button", { name: /review suggestion 1/i });
    paragraph.focus();
    await user.keyboard(" ");
    expect(onSuggestionClick).toHaveBeenCalledTimes(2);
    expect(paragraph.textContent).toBe(manuscript.paragraphs[0].text);
    expect(await axe(container)).toHaveNoViolations();
  });

  it("retains accepted-text substitution without changing other paragraphs", () => {
    renderEditor([{ ...suggestion, status: "Accepted" }]);
    expect([...screen.getByRole("document").querySelectorAll("p")].map((p) => p.textContent))
      .toEqual([suggestion.proposedChange, manuscript.paragraphs[1].text, ""]);
  });

  it("groups sibling markers without stacking controls over adjacent paragraphs", async () => {
    const user = userEvent.setup();
    const { onSuggestionClick } = renderEditor([
      suggestion, { ...suggestion, id: "s-2", status: "Rejected", rationale: "Retain this sentence." },
    ]);
    const group = screen.getByRole("button", { name: /suggestion 1, plus 1 more/i });
    expect(screen.queryByRole("button", { name: /suggestion 2, rejected/i })).not.toBeInTheDocument();
    group.focus();
    await user.keyboard("{Enter}");
    expect(await screen.findByRole("menuitem", { name: /pending/i })).toHaveFocus();
    await user.keyboard("{ArrowDown}{Enter}");
    expect(onSuggestionClick).toHaveBeenCalledWith("s-2");
    expect(group).toHaveFocus();
    expect(screen.queryByRole("menu")).not.toBeInTheDocument();
    expect(screen.getByRole("document").querySelectorAll("p")).toHaveLength(3);
    await user.keyboard("{Enter}{Escape}");
    expect(screen.queryByRole("menu")).not.toBeInTheDocument();
    expect(group).toHaveFocus();
  });
});
