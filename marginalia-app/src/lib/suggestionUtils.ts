import type { Suggestion, Paragraph } from "@/types";

/**
 * Applies accepted suggestions to paragraphs and returns the transformed full text.
 * Each accepted suggestion replaces the text of its target paragraph.
 */
export function applyAcceptedSuggestions(
  paragraphs: readonly Paragraph[],
  suggestions: readonly Suggestion[]
): string {
  const acceptedByParagraph = new Map<string, Suggestion>();
  for (const s of suggestions) {
    if (s.status === "Accepted" && !acceptedByParagraph.has(s.paragraphId)) {
      acceptedByParagraph.set(s.paragraphId, s);
    }
  }

  return paragraphs
    .map((p) => {
      const accepted = acceptedByParagraph.get(p.id);
      return accepted ? accepted.proposedChange : p.text;
    })
    .join("\n\n");
}

export function getAcceptedSuggestionsCharacterCount(
  paragraphs: readonly Paragraph[],
  suggestions: readonly Suggestion[]
): number {
  return applyAcceptedSuggestions(paragraphs, suggestions).length;
}