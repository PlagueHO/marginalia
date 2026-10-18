import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { toast } from "sonner";
import { ExportControls } from "@/components/ExportControls";
import * as documentService from "@/services/documentService";

vi.mock("@/services/documentService", () => ({ exportDocument: vi.fn() }));
vi.mock("sonner", () => ({ toast: { error: vi.fn() } }));

describe("ExportControls", () => {
  it("reports export failures and enables retry", async () => {
    vi.mocked(documentService.exportDocument).mockRejectedValueOnce(new Error("Unavailable"));
    const user = userEvent.setup();
    render(<ExportControls documentId="doc-1" filename="sample.docx" />);
    const button = screen.getByRole("button", { name: "Export" });
    expect(button).toHaveAttribute("data-variant", "outline");
    await user.click(button);
    await waitFor(() => expect(toast.error).toHaveBeenCalledWith("Export failed. Please try again."));
    expect(button).toBeEnabled();
  });

  it("downloads the revised document and releases its blob URL", async () => {
    vi.mocked(documentService.exportDocument).mockResolvedValueOnce(new Blob(["document"]));
    const createUrl = vi.spyOn(URL, "createObjectURL").mockReturnValue("blob:review");
    const revokeUrl = vi.spyOn(URL, "revokeObjectURL").mockImplementation(() => {});
    const download = vi.spyOn(HTMLAnchorElement.prototype, "click").mockImplementation(() => {});
    try {
      const user = userEvent.setup();
      render(<ExportControls documentId="doc-1" filename="sample.docx" />);
      await user.click(screen.getByRole("button", { name: "Export" }));
      expect(documentService.exportDocument).toHaveBeenLastCalledWith("doc-1");
      expect(download).toHaveBeenCalledOnce();
      expect(revokeUrl).toHaveBeenCalledWith("blob:review");
    } finally {
      createUrl.mockRestore();
      revokeUrl.mockRestore();
      download.mockRestore();
    }
  });
});
