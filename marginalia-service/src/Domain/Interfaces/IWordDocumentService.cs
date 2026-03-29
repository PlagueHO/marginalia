using Marginalia.Domain.Models;

namespace Marginalia.Domain.Interfaces;

/// <summary>
/// Contract for Word document (.docx) import and export operations.
/// </summary>
public interface IWordDocumentService
{
    /// <summary>
    /// Parses a .docx file stream and returns a Document with extracted text content.
    /// </summary>
    Task<Document> ParseAsync(Stream fileStream, string filename, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a Document back to .docx format.
    /// </summary>
    Task<Stream> ExportAsync(Document document, CancellationToken cancellationToken = default);
}
