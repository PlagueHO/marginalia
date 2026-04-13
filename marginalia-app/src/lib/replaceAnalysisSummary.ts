/**
 * Builds the primary summary line shown in the replace-analysis confirmation.
 * Describes what will be discarded and what will be merged.
 */
export function buildSummaryMessage(
  acceptedCount: number,
  pendingCount: number,
  rejectedCount: number,
): string {
  const discardParts: string[] = [];
  if (pendingCount > 0) {
    discardParts.push(`${pendingCount} pending`);
  }
  if (rejectedCount > 0) {
    discardParts.push(`${rejectedCount} rejected`);
  }

  const discardLabel = discardParts.length > 0
    ? discardParts.join(" and ")
    : null;

  const plural = (count: number) => (count !== 1 ? "s" : "");

  if (acceptedCount > 0 && discardLabel) {
    return `Your ${acceptedCount} accepted suggestion${plural(acceptedCount)} will be merged into the manuscript. The ${discardLabel} suggestion${plural(pendingCount + rejectedCount)} will be discarded.`;
  }

  if (acceptedCount > 0) {
    return `Your ${acceptedCount} accepted suggestion${plural(acceptedCount)} will be merged into the manuscript before re-analysis.`;
  }

  if (discardLabel) {
    return `All ${discardLabel} suggestion${plural(pendingCount + rejectedCount)} will be discarded and replaced with new analysis results.`;
  }

  return "All existing suggestions will be replaced with new analysis results.";
}