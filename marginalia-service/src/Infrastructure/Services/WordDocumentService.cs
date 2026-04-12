using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using DomainDocument = Marginalia.Domain.Models.Document;
using DomainParagraph = Marginalia.Domain.Models.Paragraph;
using OpenXmlParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;

namespace Marginalia.Infrastructure.Services;

/// <summary>
/// Handles .docx import and export using OpenXml SDK.
/// </summary>
public sealed class WordDocumentService : IWordDocumentService
{
    public Task<DomainDocument> ParseAsync(Stream fileStream, string filename, CancellationToken cancellationToken = default)
    {
        using var wordDoc = WordprocessingDocument.Open(fileStream, false);
        var body = wordDoc.MainDocumentPart?.Document?.Body;

        if (body is null)
        {
            throw new InvalidOperationException("The uploaded Word document has no content.");
        }

        var paragraphs = body.Elements<OpenXmlParagraph>()
            .Select(p => p.InnerText)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Select(text => new DomainParagraph
            {
                Id = Guid.NewGuid().ToString("N"),
                Text = text.Trim()
            })
            .ToList()
            .AsReadOnly();

        var document = new DomainDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            Filename = filename,
            Source = DocumentSource.Local,
            Paragraphs = paragraphs
        };

        return Task.FromResult(document);
    }

    public Task<Stream> ExportAsync(DomainDocument document, CancellationToken cancellationToken = default)
    {
        var memoryStream = new MemoryStream();

        using (var wordDoc = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
            var body = mainPart.Document.AppendChild(new Body());

            var finalParagraphs = ApplySuggestions(document);

            foreach (var paraText in finalParagraphs)
            {
                var paragraph = new OpenXmlParagraph();
                var run = new Run();
                run.AppendChild(new Text(paraText) { Space = SpaceProcessingModeValues.Preserve });
                paragraph.AppendChild(run);
                body.AppendChild(paragraph);
            }

            mainPart.Document.Save();
        }

        memoryStream.Position = 0;
        return Task.FromResult<Stream>(memoryStream);
    }

    private static List<string> ApplySuggestions(DomainDocument document)
    {
        var suggestionsByParagraph = document.Suggestions
            .Where(s => s.Status == SuggestionStatus.Accepted || s.Status == SuggestionStatus.Modified)
            .GroupBy(s => s.ParagraphId)
            .ToDictionary(g => g.Key, g => g.First());

        return document.Paragraphs.Select(p =>
        {
            if (suggestionsByParagraph.TryGetValue(p.Id, out var suggestion))
            {
                return suggestion.Status == SuggestionStatus.Modified
                    ? (suggestion.UserSteeringInput ?? suggestion.ProposedChange)
                    : suggestion.ProposedChange;
            }

            return p.Text;
        }).ToList();
    }
}
