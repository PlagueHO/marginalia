# Marginalia User Guide

Marginalia is an AI-powered manuscript analysis tool that helps writers improve
their work through intelligent suggestions. This guide walks you through the
full workflow: creating a manuscript, analyzing it, and reviewing suggestions.

## Getting Started

When you first open Marginalia, you see the **Your Manuscripts** page. This is
your home screen where all saved manuscripts are listed. If you have no
manuscripts yet, you will see an empty state prompting you to create one.

![Home page showing empty manuscript list](images/01-home-page.png)

## Creating a New Manuscript

1. Click the **New Manuscript** button on the home page, or select the **New**
   tab in the navigation bar.

You are presented with two options for adding content:

- **Upload a Word document** (.docx) by dragging and dropping into the upload
  zone or clicking to browse.
- **Paste text** directly using the paste text mode.

![New manuscript page with upload and paste options](images/02-new-manuscript.png)

### Using Paste Mode

1. Enter a title for your manuscript in the **Manuscript title** field.
1. Click the **Paste text instead** button below the upload zone.

![Paste text panel expanded with title entered](images/03-paste-text-mode.png)

1. Paste or type your manuscript text into the text area.
1. Click the **Load Text** button to import the text.

![Text pasted into the text area ready to load](images/04-text-pasted.png)

## The Editor

After loading your text, Marginalia opens the **Editor** view. The editor
displays:

- **Toolbar** — shows the manuscript title, source label, original and accepted
  word counts, and action buttons (Analyze, Export, Delete).
- **Document pane** (left) — your manuscript text, divided into paragraphs.
- **Suggestions panel** (right) — where AI suggestions appear after analysis.

![Editor view with loaded manuscript and empty suggestions panel](images/05-editor-loaded.png)

## Analyzing Your Manuscript

1. Click the **Analyze** button in the toolbar. The **Analyze Manuscript**
   dialog opens.

![Analyze Manuscript dialog with tone and guidance fields](images/06-analyze-dialog.png)

1. Select a **Tone** from the dropdown. Available tones include Professional,
   Narrative, Academic, Conversational, and Literary.
1. Optionally enter **Guidance** to focus the analysis on specific aspects of
   your writing.

In this example, we select **Narrative** tone and enter "Fantasy role playing"
as guidance.

![Analyze dialog configured with Narrative tone and guidance](images/07-analyze-configured.png)

1. Click **Analyze** to start the AI analysis. A progress indicator shows while
   the analysis runs.

![Analysis in progress with spinner](images/08-analyzing.png)

## Reviewing Suggestions

Once the analysis completes, suggestions appear in the right panel. Each
suggestion is linked to a paragraph in your manuscript, indicated by numbered
markers in the document.

The suggestions panel shows:

- **Summary counts** — pending, accepted, and rejected suggestions.
- **Bulk actions** — Accept All and Reject All buttons.
- **Filter tabs** — All, Pending, Accepted (check mark), Rejected (X), and
  Modified (pencil).
- **Individual suggestion cards** — each showing the paragraph number, status,
  and a summary of the feedback.

![Analysis complete with 3 suggestions in the panel](images/09-analysis-results.png)

### Expanding a Suggestion

Click a suggestion card or its expand arrow to see the full details:

- **Original text** — the current paragraph text (shown with strikethrough).
- **Proposed change** — the AI-recommended replacement text.
- **Action buttons** — Accept, Reject, Modify, or re-Analyze.

![Suggestion expanded showing original text and proposed change](images/10-suggestion-expanded.png)

### Accepting a Suggestion

Click **Accept** on a suggestion to apply the proposed change. The document
text updates immediately to reflect the accepted revision, and the word count
adjusts in the toolbar.

![Suggestion 1 accepted with updated text in the document](images/11-suggestion-accepted.png)

You can accept multiple suggestions individually. In this example, suggestions 1
and 3 have been accepted while suggestion 2 remains pending.

![Two suggestions accepted, one still pending](images/13-suggestions-accepted.png)

### Other Suggestion Actions

- **Reject** — dismisses the suggestion without changing the text.
- **Modify** — lets you edit the proposed text before applying it.
- **Revert to Pending** — undoes an accept or reject, returning the suggestion
  to pending status.
- **Analyze** — re-runs the analysis on just that paragraph.

## Re-Analyzing a Manuscript

You can run a fresh analysis on a manuscript that already has suggestions. This
is useful when you have accepted some suggestions and want new feedback on the
updated text.

1. Open the manuscript in the editor. In this example, 2 suggestions have been
   accepted and 1 is still pending.

![Editor showing 2 accepted and 1 pending suggestion before re-analysis](images/15-before-reanalysis.png)

1. Click **Analyze** in the toolbar. The Analyze Manuscript dialog opens,
   allowing you to choose a new tone and guidance.

![Analyze dialog opened for re-analysis](images/16-reanalysis-dialog.png)

1. Configure the tone and guidance as desired, then click **Analyze**.

![Re-analysis dialog configured with Narrative tone](images/17-reanalysis-configured.png)

1. Because the manuscript was analyzed before, a **Replace Analysis?**
   confirmation dialog appears. It explains that your accepted suggestions will
   be merged into the manuscript text and any pending suggestions will be
   discarded.

![Replace Analysis confirmation showing merge details](images/18-replace-analysis-dialog.png)

1. Click **Replace & Analyze** to proceed. The accepted changes become part of
   the base text, and a new set of suggestions is generated.

![New analysis results with 3 fresh pending suggestions](images/19-reanalysis-results.png)

The original word count now reflects the merged text, and all suggestions start
fresh in the pending state.

## Analyzing a Single Paragraph

Instead of re-analyzing the entire manuscript, you can re-analyze a single
paragraph from within an expanded suggestion. This lets you try different tone
and guidance settings on just one section.

1. Expand a suggestion by clicking its card or the expand arrow.

![Suggestion expanded showing original and proposed text with Analyze button](images/20-suggestion-expanded-for-reanalyze.png)

1. Click the **Analyze** button at the bottom of the expanded suggestion. The
   **Analyze Paragraph** dialog opens, pre-populated with the settings from the
   last analysis.

![Analyze Paragraph dialog pre-populated with previous settings](images/21-analyze-paragraph-dialog.png)

1. Change the tone and guidance to explore a different editorial direction. In
   this example, we switch to **Conversational** tone and enter "Childrens
   story" as guidance.

![Analyze Paragraph dialog with Conversational tone and Childrens story guidance](images/22-paragraph-analyze-configured.png)

1. Click **Analyze**. A new suggestion is generated for just that paragraph and
   added alongside the existing suggestions. The paragraph now shows multiple
   suggestion markers, and the suggestion count increases.

![New per-paragraph suggestion with children's story tone alongside original](images/23-paragraph-reanalysis-result.png)

You can repeat this process with different tones and guidance to compare multiple
editorial perspectives for the same paragraph.

## Managing Manuscripts

After analysis, your manuscript appears in the manuscript list on the home page,
showing the title, date, suggestion count, and analysis status.

![Manuscript list showing the analyzed document](images/14-manuscript-list.png)

From the editor toolbar you can also:

- **Export** — download the manuscript with accepted changes applied.
- **Delete** — remove the manuscript from your library.
