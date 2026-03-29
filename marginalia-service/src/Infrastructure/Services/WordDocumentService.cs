using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using DomainDocument = Marginalia.Domain.Models.Document;

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

        var paragraphs = body.Elements<Paragraph>();
        var content = string.Join("\n\n", paragraphs
            .Select(p => p.InnerText)
            .Where(text => !string.IsNullOrWhiteSpace(text)));

        var document = new DomainDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            Filename = filename,
            Source = DocumentSource.Local,
            Content = content
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

            // Apply accepted suggestions to produce final content
            var finalContent = ApplySuggestions(document);

            var paragraphs = finalContent.Split(["\n\n"], StringSplitOptions.None);
            foreach (var paraText in paragraphs)
            {
                var paragraph = new Paragraph();
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

    private static string ApplySuggestions(DomainDocument document)
    {
        var content = document.Content;
        var accepted = document.Suggestions
            .Where(s => s.Status == SuggestionStatus.Accepted)
            .OrderByDescending(s => s.TextRange.Start)
            .ToList();

        foreach (var suggestion in accepted)
        {
            var start = Math.Max(0, Math.Min(suggestion.TextRange.Start, content.Length));
            var end = Math.Max(start, Math.Min(suggestion.TextRange.End, content.Length));
            content = string.Concat(content.AsSpan(0, start), suggestion.ProposedChange, content.AsSpan(end));
        }

        return content;
    }
}
