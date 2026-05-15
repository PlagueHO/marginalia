using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using Marginalia.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Marginalia.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DocumentsController : ControllerBase
{
    private const long MaxArchiveSizeBytes = 52_428_800; // 50 MB
    private static readonly JsonSerializerOptions ArchiveSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IDocumentRepository _documentRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly ISuggestionService _suggestionService;
    private readonly IWordDocumentService _wordDocumentService;
    private readonly SuggestionMergeService _suggestionMergeService;
    private readonly ILogger<DocumentsController> _logger;
    private readonly Func<bool> _isMultiUserModeProvider;

    public DocumentsController(
        IDocumentRepository documentRepository,
        ISessionRepository sessionRepository,
        ISuggestionService suggestionService,
        IWordDocumentService wordDocumentService,
        SuggestionMergeService suggestionMergeService,
        ILogger<DocumentsController> logger,
        Func<bool>? isMultiUserModeProvider = null)
    {
        _documentRepository = documentRepository;
        _sessionRepository = sessionRepository;
        _suggestionService = suggestionService;
        _wordDocumentService = wordDocumentService;
        _suggestionMergeService = suggestionMergeService;
        _logger = logger;
        _isMultiUserModeProvider = isMultiUserModeProvider ?? IsMultiUserModeFromEnvironment;
    }

    private static string GetUserId(HttpRequest request)
    {
        if (request.Headers.TryGetValue("X-User-Id", out var userIdHeader) &&
            !string.IsNullOrWhiteSpace(userIdHeader.ToString()))
        {
            return userIdHeader.ToString();
        }
        return "_anonymous";
    }

    private static bool IsMultiUserModeFromEnvironment()
    {
        if (bool.TryParse(Environment.GetEnvironmentVariable("ENABLE_ENTRA_AUTH"), out var entraAuthEnabled))
        {
            return entraAuthEnabled;
        }

        return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AzureAd__ClientId"));
    }

    /// <summary>
    /// List all documents for the current user.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DocumentListResponse>> List(CancellationToken cancellationToken)
    {
        var userId = GetUserId(Request);
        var documents = await _documentRepository.GetByUserAsync(userId, cancellationToken);

        var summaries = documents
            .OrderByDescending(d => d.UpdatedAt)
            .Select(d =>
            {
                var suggestions = d.Suggestions ?? [];
                var paragraphs = d.Paragraphs ?? [];

                return new DocumentSummary
                {
                    Id = d.Id,
                    Title = string.IsNullOrEmpty(d.Title) ? d.Filename : d.Title,
                    Filename = d.Filename,
                    Source = d.Source,
                    Status = suggestions.Count > 0 ? DocumentStatus.Analyzed : d.Status,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt,
                    SuggestionCount = suggestions.Count,
                    ParagraphCount = paragraphs.Count
                };
            })
            .ToList()
            .AsReadOnly();

        _logger.LogInformation("Listed {Count} documents for UserId: {UserId}", summaries.Count, userId);

        return Ok(new DocumentListResponse { Documents = summaries });
    }

    /// <summary>
    /// Upload a Word document (.docx) for analysis.
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(52_428_800)] // 50 MB
    public async Task<ActionResult<UploadDocumentResponse>> Upload(IFormFile file, [FromForm] string? title, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "No file provided." });
        }

        if (!file.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Upload rejected — unsupported file type: {FileName}", file.FileName);
            return BadRequest(new { error = "Only .docx files are supported." });
        }

        var userId = GetUserId(Request);
        var now = DateTimeOffset.UtcNow;

        using var stream = file.OpenReadStream();
        var document = await _wordDocumentService.ParseAsync(stream, file.FileName, cancellationToken);

        // Set userId and new metadata fields
        document = document with
        {
            UserId = userId,
            Title = title ?? $"{now:yyyy-MM-dd HH:mm} - {file.FileName}",
            Status = DocumentStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _documentRepository.SaveAsync(document, cancellationToken);

        var session = new UserSession
        {
            SessionId = Guid.NewGuid().ToString("N"),
            UserId = userId,
            DocumentIds = [document.Id],
            Timestamp = DateTimeOffset.UtcNow
        };
        await _sessionRepository.SaveAsync(session, cancellationToken);

        _logger.LogInformation("Document uploaded: {DocumentId}, FileName: {FileName}, Size: {Size} bytes, SessionId: {SessionId}, UserId: {UserId}", document.Id, file.FileName, file.Length, session.SessionId, userId);

        var response = new UploadDocumentResponse { Document = document, SessionId = session.SessionId };
        return CreatedAtAction(nameof(GetById), new { id = document.Id }, response);
    }

    /// <summary>
    /// Create a document from pasted text.
    /// </summary>
    [HttpPost("paste")]
    [RequestSizeLimit(52_428_800)] // 50 MB
    public async Task<ActionResult<UploadDocumentResponse>> Paste([FromBody] PasteDocumentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { error = "Content cannot be empty." });
        }

        var userId = GetUserId(Request);
        var now = DateTimeOffset.UtcNow;

        // Split pasted text into paragraphs on double-newline boundaries
        var paragraphs = SplitIntoParagraphs(request.Content);

        var document = new Document
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            Filename = request.Filename ?? "pasted-text.txt",
            Source = DocumentSource.Local,
            Paragraphs = paragraphs,
            Title = request.Title ?? $"{now:yyyy-MM-dd HH:mm} - {request.Filename ?? "Untitled"}",
            Status = DocumentStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _documentRepository.SaveAsync(document, cancellationToken);

        var session = new UserSession
        {
            SessionId = Guid.NewGuid().ToString("N"),
            UserId = userId,
            DocumentIds = [document.Id],
            Timestamp = DateTimeOffset.UtcNow
        };
        await _sessionRepository.SaveAsync(session, cancellationToken);

        _logger.LogInformation("Document created from paste: {DocumentId}, FileName: {FileName}, SessionId: {SessionId}, UserId: {UserId}", document.Id, document.Filename, session.SessionId, userId);

        var response = new UploadDocumentResponse { Document = document, SessionId = session.SessionId };
        return CreatedAtAction(nameof(GetById), new { id = document.Id }, response);
    }

    /// <summary>
    /// Get a document by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Document>> GetById(string id, CancellationToken cancellationToken)
    {
        var userId = GetUserId(Request);
        var document = await _documentRepository.GetByIdAsync(userId, id, cancellationToken);
        if (document is null)
        {
            _logger.LogWarning("Document not found: {DocumentId}, UserId: {UserId}", id, userId);
            return NotFound(new { error = $"Document '{id}' not found." });
        }

        return Ok(document);
    }

    /// <summary>
    /// Get all suggestions for a document.
    /// </summary>
    [HttpGet("{id}/suggestions")]
    public async Task<ActionResult<IReadOnlyList<Suggestion>>> GetSuggestions(string id, CancellationToken cancellationToken)
    {
        var userId = GetUserId(Request);
        var document = await _documentRepository.GetByIdAsync(userId, id, cancellationToken);
        if (document is null)
        {
            _logger.LogWarning("Document not found for suggestions: {DocumentId}, UserId: {UserId}", id, userId);
            return NotFound(new { error = $"Document '{id}' not found." });
        }

        return Ok(document.Suggestions);
    }

    /// <summary>
    /// Trigger AI analysis on a document, returns generated suggestions.
    /// If the document has been previously analyzed and the user confirms via the frontend,
    /// accepted suggestions are merged into the paragraphs and all non-accepted suggestions are deleted
    /// before running the fresh analysis.
    /// </summary>
    [HttpPost("{id}/analyze")]
    public async Task<ActionResult<IReadOnlyList<Suggestion>>> Analyze(
        string id,
        [FromBody] AnalysisRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(Request);

        var document = await _documentRepository.GetByIdAsync(userId, id, cancellationToken);
        if (document is null)
        {
            _logger.LogWarning("Document not found for analysis: {DocumentId}, UserId: {UserId}", id, userId);
            return NotFound(new { error = $"Document '{id}' not found." });
        }

        var paragraphsForAnalysis = document.Paragraphs;
        int mergedSuggestionCount = 0;

        // Handle re-analysis: merge accepted suggestions into paragraphs and clear all suggestions
        if (document.Status == DocumentStatus.Analyzed && document.Suggestions.Count > 0)
        {
            var acceptedSuggestions = document.Suggestions
                .Where(s => s.Status == SuggestionStatus.Accepted || s.Status == SuggestionStatus.Modified)
                .ToList();

            var nonAcceptedSuggestions = document.Suggestions
                .Where(s => s.Status != SuggestionStatus.Accepted && s.Status != SuggestionStatus.Modified)
                .ToList();

            // Apply accepted suggestions to paragraphs
            if (acceptedSuggestions.Count > 0)
            {
                paragraphsForAnalysis = _suggestionMergeService.ApplyAcceptedSuggestionsToParagraphs(
                    document.Paragraphs,
                    acceptedSuggestions.AsReadOnly());
                mergedSuggestionCount = acceptedSuggestions.Count;
            }

            _logger.LogInformation(
                "Re-analysis requested for document: {DocumentId}, Merged: {MergedCount}, Cleared: {ClearedCount}, UserId: {UserId}",
                id,
                mergedSuggestionCount,
                nonAcceptedSuggestions.Count,
                userId);

            document = document with
            {
                Paragraphs = paragraphsForAnalysis,
                Suggestions = []
            };
        }
        else
        {
            _logger.LogInformation("Analysis requested for document: {DocumentId}, ParagraphCount: {ParagraphCount}, UserId: {UserId}", id, document.Paragraphs.Count, userId);
        }

        var userGuidance = CombineGuidance(request?.EffectiveUserInstructions, request?.EffectiveToneGuidance);

        IReadOnlyList<Suggestion> suggestions;
        try
        {
            suggestions = await _suggestionService.AnalyzeAsync(
                document.Id,
                paragraphsForAnalysis,
                userGuidance,
                cancellationToken);
        }
        catch (ContentFilterException ex)
        {
            var triggeredCategories = ex.FilterResults
                .Where(f => f.Filtered)
                .Select(f => new { category = f.Category, severity = f.Severity })
                .ToList();

            var categorySummary = triggeredCategories.Count > 0
                ? string.Join(", ", triggeredCategories.Select(c =>
                    c.severity is not null ? $"{c.category}({c.severity})" : c.category))
                : "(none)";

            _logger.LogWarning(
                "Content filter triggered for document: {DocumentId}, UserId: {UserId}, Categories: {Categories}",
                id,
                userId,
                categorySummary);

            return UnprocessableEntity(new
            {
                error = "The document content was blocked by the AI content safety filter.",
                code = "content_filter",
                categories = triggeredCategories
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analysis failed for document: {DocumentId}, UserId: {UserId}", id, userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Analysis failed. Please try again." });
        }

        var updatedDocument = document with
        {
            Suggestions = suggestions,
            Status = DocumentStatus.Analyzed,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _documentRepository.SaveAsync(updatedDocument, cancellationToken);

        _logger.LogInformation("Analysis complete for document: {DocumentId}, SuggestionsGenerated: {Count}, MergedSuggestions: {MergedCount}", id, suggestions.Count, mergedSuggestionCount);

        return Ok(suggestions);
    }

    /// <summary>
    /// Trigger AI analysis on a single paragraph within a document.
    /// Returns new suggestions for the targeted paragraph (additive — existing suggestions are preserved).
    /// </summary>
    [HttpPost("{id}/paragraphs/{paragraphId}/analyze")]
    public async Task<ActionResult<IReadOnlyList<Suggestion>>> AnalyzeParagraph(
        string id,
        string paragraphId,
        [FromBody] AnalysisRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(Request);

        var document = await _documentRepository.GetByIdAsync(userId, id, cancellationToken);
        if (document is null)
        {
            _logger.LogWarning("Document not found for paragraph analysis: {DocumentId}, UserId: {UserId}", id, userId);
            return NotFound(new { error = $"Document '{id}' not found." });
        }

        var targetParagraph = document.Paragraphs.FirstOrDefault(p => p.Id == paragraphId);
        if (targetParagraph is null)
        {
            _logger.LogWarning("Paragraph not found for analysis: {ParagraphId}, DocumentId: {DocumentId}", paragraphId, id);
            return NotFound(new { error = $"Paragraph '{paragraphId}' not found in document '{id}'." });
        }

        // Gather context: up to 2 paragraphs before and after the target
        var targetIndex = document.GetParagraphIndex(paragraphId);
        var contextStart = Math.Max(0, targetIndex - 2);
        var contextEnd = Math.Min(document.Paragraphs.Count - 1, targetIndex + 2);
        var contextParagraphs = new List<Paragraph>();
        for (var i = contextStart; i <= contextEnd; i++)
        {
            if (i != targetIndex)
            {
                contextParagraphs.Add(document.Paragraphs[i]);
            }
        }

        var userGuidance = CombineGuidance(request?.EffectiveUserInstructions, request?.EffectiveToneGuidance);

        IReadOnlyList<Suggestion> newSuggestions;
        try
        {
            newSuggestions = await _suggestionService.AnalyzeParagraphAsync(
                document.Id,
                targetParagraph,
                contextParagraphs.AsReadOnly(),
                userGuidance,
                cancellationToken);
        }
        catch (ContentFilterException ex)
        {
            var triggeredCategories = ex.FilterResults
                .Where(f => f.Filtered)
                .Select(f => new { category = f.Category, severity = f.Severity })
                .ToList();

            var categorySummary = triggeredCategories.Count > 0
                ? string.Join(", ", triggeredCategories.Select(c =>
                    c.severity is not null ? $"{c.category}({c.severity})" : c.category))
                : "(none)";

            _logger.LogWarning(
                "Content filter triggered for paragraph: {ParagraphId}, DocumentId: {DocumentId}, UserId: {UserId}, Categories: {Categories}",
                paragraphId,
                id,
                userId,
                categorySummary);

            return UnprocessableEntity(new
            {
                error = "The document content was blocked by the AI content safety filter.",
                code = "content_filter",
                categories = triggeredCategories
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Paragraph analysis failed: {ParagraphId}, DocumentId: {DocumentId}, UserId: {UserId}", paragraphId, id, userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Analysis failed. Please try again." });
        }

        // Reset previously accepted/modified suggestions for this paragraph back to pending
        // so the newly generated suggestions become the current options to review.
        var normalizedExistingSuggestions = document.Suggestions
            .Select(s =>
                s.ParagraphId == paragraphId &&
                (s.Status == SuggestionStatus.Accepted || s.Status == SuggestionStatus.Modified)
                    ? s with { Status = SuggestionStatus.Pending }
                    : s)
            .ToList();

        // Additive: append new suggestions to existing ones.
        var allSuggestions = normalizedExistingSuggestions
            .Concat(newSuggestions)
            .ToList()
            .AsReadOnly();

        var updatedDocument = document with
        {
            Suggestions = allSuggestions,
            Status = DocumentStatus.Analyzed,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _documentRepository.SaveAsync(updatedDocument, cancellationToken);

        _logger.LogInformation(
            "Paragraph analysis complete: {ParagraphId}, DocumentId: {DocumentId}, NewSuggestions: {Count}, UserId: {UserId}",
            paragraphId, id, newSuggestions.Count, userId);

        return Ok(newSuggestions);
    }

    /// <summary>
    /// Update a suggestion's status (accept, reject, modify).
    /// When accepting a suggestion, all other suggestions targeting the same paragraph
    /// are automatically rejected (exclusive acceptance).
    /// </summary>
    [HttpPut("{id}/suggestions/{suggestionId}")]
    public async Task<ActionResult<Suggestion>> UpdateSuggestion(
        string id,
        string suggestionId,
        [FromBody] UpdateSuggestionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(Request);
        var document = await _documentRepository.GetByIdAsync(userId, id, cancellationToken);
        if (document is null)
        {
            return NotFound(new { error = $"Document '{id}' not found." });
        }

        var suggestion = document.Suggestions.FirstOrDefault(s => s.Id == suggestionId);
        if (suggestion is null)
        {
            return NotFound(new { error = $"Suggestion '{suggestionId}' not found." });
        }

        var updated = suggestion with
        {
            Status = request.Status,
            UserSteeringInput = request.UserSteeringInput ?? suggestion.UserSteeringInput
        };

        var isAccepting = request.Status == SuggestionStatus.Accepted || request.Status == SuggestionStatus.Modified;

        var updatedSuggestions = document.Suggestions
            .Select(s =>
            {
                if (s.Id == suggestionId)
                {
                    return updated;
                }

                // Exclusive acceptance: auto-reject all other suggestions on the same paragraph.
                if (isAccepting &&
                    s.ParagraphId == suggestion.ParagraphId &&
                    s.Status != SuggestionStatus.Rejected)
                {
                    return s with { Status = SuggestionStatus.Rejected };
                }

                return s;
            })
            .ToList()
            .AsReadOnly();

        var updatedDocument = document with { Suggestions = updatedSuggestions };
        await _documentRepository.SaveAsync(updatedDocument, cancellationToken);

        return Ok(updated);
    }

    /// <summary>
    /// Update the title of a document.
    /// </summary>
    [HttpPut("{id}/title")]
    public async Task<ActionResult<Document>> UpdateTitle(
        string id,
        [FromBody] UpdateDocumentTitleRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { error = "Title cannot be empty." });
        }

        var userId = GetUserId(Request);
        var document = await _documentRepository.GetByIdAsync(userId, id, cancellationToken);
        if (document is null)
        {
            _logger.LogWarning("Document not found for title update: {DocumentId}, UserId: {UserId}", id, userId);
            return NotFound(new { error = $"Document '{id}' not found." });
        }

        var updatedDocument = document with
        {
            Title = request.Title,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _documentRepository.SaveAsync(updatedDocument, cancellationToken);

        _logger.LogInformation("Document title updated: {DocumentId}, Title: {Title}, UserId: {UserId}", id, request.Title, userId);

        return Ok(updatedDocument);
    }

    /// <summary>
    /// Delete a document by ID.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var userId = GetUserId(Request);
        var document = await _documentRepository.GetByIdAsync(userId, id, cancellationToken);
        if (document is null)
        {
            _logger.LogWarning("Document not found for deletion: {DocumentId}, UserId: {UserId}", id, userId);
            return NotFound(new { error = $"Document '{id}' not found." });
        }

        await _documentRepository.DeleteAsync(userId, id, cancellationToken);

        _logger.LogInformation("Document deleted: {DocumentId}, UserId: {UserId}", id, userId);

        return NoContent();
    }

    /// <summary>
    /// Export manuscripts as a ZIP archive.
    /// In single-user mode exports all manuscripts; in multi-user mode exports only the current user's manuscripts.
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportAll(CancellationToken cancellationToken)
    {
        var userId = GetUserId(Request);
        var multiUserMode = _isMultiUserModeProvider();

        if (multiUserMode && string.Equals(userId, "_anonymous", StringComparison.Ordinal))
        {
            _logger.LogWarning("Bulk export rejected in multi-user mode due to missing user identity.");
            return Unauthorized(new { error = "X-User-Id header is required in multi-user mode." });
        }

        var documents = multiUserMode
            ? await _documentRepository.GetByUserAsync(userId, cancellationToken)
            : await _documentRepository.GetAllAsync(cancellationToken);

        var exportDocuments = multiUserMode
            ? SanitizeMultiUserExport(documents, userId)
            : documents;

        var archiveStream = await BuildArchiveAsync(exportDocuments, cancellationToken);

        _logger.LogInformation(
            "Bulk export requested: {DocumentCount} documents, UserId: {UserId}, MultiUserMode: {MultiUserMode}",
            exportDocuments.Count,
            userId,
            multiUserMode);

        return File(
            archiveStream,
            "application/zip",
            $"manuscripts-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.zip");
    }

    /// <summary>
    /// Import manuscripts from a ZIP archive.
    /// In single-user mode preserves source ownership; in multi-user mode imports all manuscripts under the current user.
    /// </summary>
    [HttpPost("import")]
    [RequestSizeLimit(MaxArchiveSizeBytes)]
    public async Task<ActionResult<ImportDocumentsResponse>> Import(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "No file provided." });
        }

        if (file.Length > MaxArchiveSizeBytes)
        {
            return BadRequest(new { error = "Archive exceeds the 50 MB size limit." });
        }

        if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Only .zip files are supported for manuscript import." });
        }

        var userId = GetUserId(Request);
        var multiUserMode = _isMultiUserModeProvider();

        if (multiUserMode && string.Equals(userId, "_anonymous", StringComparison.Ordinal))
        {
            _logger.LogWarning("Bulk import rejected in multi-user mode due to missing user identity.");
            return Unauthorized(new { error = "X-User-Id header is required in multi-user mode." });
        }

        await using var archiveStream = file.OpenReadStream();
        var documents = await ReadArchiveAsync(archiveStream, cancellationToken);
        if (documents is null)
        {
            _logger.LogWarning("Bulk import rejected: invalid archive format. UserId: {UserId}", userId);
            return BadRequest(new { error = "Archive must contain exactly one manuscripts.json file with valid document data." });
        }

        var importedCount = 0;
        var importedAt = DateTimeOffset.UtcNow;

        foreach (var document in documents)
        {
            var normalizedDocument = NormalizeImportedDocument(document, userId, multiUserMode, importedAt);
            await _documentRepository.SaveAsync(normalizedDocument, cancellationToken);
            importedCount++;
        }

        _logger.LogInformation(
            "Bulk import completed: {ImportedCount} documents, UserId: {UserId}, MultiUserMode: {MultiUserMode}",
            importedCount,
            userId,
            multiUserMode);

        return Ok(new ImportDocumentsResponse { ImportedCount = importedCount });
    }

    /// <summary>
    /// Export the document as a .docx file with accepted suggestions applied.
    /// </summary>
    [HttpGet("{id}/export")]
    public async Task<IActionResult> Export(string id, CancellationToken cancellationToken)
    {
        var userId = GetUserId(Request);
        var document = await _documentRepository.GetByIdAsync(userId, id, cancellationToken);
        if (document is null)
        {
            _logger.LogWarning("Document not found for export: {DocumentId}, UserId: {UserId}", id, userId);
            return NotFound(new { error = $"Document '{id}' not found." });
        }

        _logger.LogInformation("Export requested for document: {DocumentId}, FileName: {FileName}, UserId: {UserId}", id, document.Filename, userId);

        var stream = await _wordDocumentService.ExportAsync(document, cancellationToken);
        var exportFilename = Path.GetFileNameWithoutExtension(document.Filename) + "-revised.docx";

        return File(stream, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", exportFilename);
    }

    private static async Task<MemoryStream> BuildArchiveAsync(
        IReadOnlyList<Document> documents,
        CancellationToken cancellationToken)
    {
        var archiveStream = new MemoryStream();

        using (var zipArchive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var manuscriptsEntry = zipArchive.CreateEntry("manuscripts.json", CompressionLevel.Optimal);
            await using var entryStream = manuscriptsEntry.Open();
            await JsonSerializer.SerializeAsync(entryStream, documents, ArchiveSerializerOptions, cancellationToken);
        }

        archiveStream.Position = 0;
        return archiveStream;
    }

    private static async Task<IReadOnlyList<Document>?> ReadArchiveAsync(
        Stream archiveStream,
        CancellationToken cancellationToken)
    {
        using var zipArchive = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: false);
        var manuscriptsEntries = zipArchive.Entries
            .Where(entry => string.Equals(entry.FullName, "manuscripts.json", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (manuscriptsEntries.Count != 1)
        {
            return null;
        }

        var manuscriptsEntry = manuscriptsEntries[0];
        if (manuscriptsEntry.Length is <= 0 or > MaxArchiveSizeBytes)
        {
            return null;
        }

        await using var entryStream = manuscriptsEntry.Open();
        var documents = await JsonSerializer.DeserializeAsync<List<Document>>(
            entryStream,
            ArchiveSerializerOptions,
            cancellationToken);

        return documents;
    }

    private static IReadOnlyList<Document> SanitizeMultiUserExport(IReadOnlyList<Document> documents, string userId)
    {
        return documents
            .Where(document => string.Equals(document.UserId, userId, StringComparison.Ordinal))
            .Select(document => document with
            {
                UserId = userId,
                Suggestions = (document.Suggestions ?? [])
                    .Where(suggestion =>
                        string.Equals(suggestion.UserId, userId, StringComparison.Ordinal) &&
                        string.Equals(suggestion.DocumentId, document.Id, StringComparison.Ordinal))
                    .ToList()
                    .AsReadOnly()
            })
            .ToList()
            .AsReadOnly();
    }

    private static Document NormalizeImportedDocument(
        Document sourceDocument,
        string currentUserId,
        bool multiUserMode,
        DateTimeOffset importedAt)
    {
        var targetUserId = multiUserMode ? currentUserId : NormalizeUserId(sourceDocument.UserId);
        var normalizedDocumentId = Guid.NewGuid().ToString("N");

        var normalizedParagraphs = (sourceDocument.Paragraphs ?? [])
            .Select(paragraph => new Paragraph
            {
                Id = string.IsNullOrWhiteSpace(paragraph.Id) ? Guid.NewGuid().ToString("N") : paragraph.Id,
                Text = paragraph.Text ?? string.Empty
            })
            .ToList()
            .AsReadOnly();

        var validParagraphIds = normalizedParagraphs
            .Select(paragraph => paragraph.Id)
            .ToHashSet(StringComparer.Ordinal);

        var normalizedSuggestions = (sourceDocument.Suggestions ?? [])
            .Select(suggestion => new Suggestion
            {
                Id = Guid.NewGuid().ToString("N"),
                UserId = targetUserId,
                DocumentId = normalizedDocumentId,
                ParagraphId = validParagraphIds.Contains(suggestion.ParagraphId) ? suggestion.ParagraphId : string.Empty,
                Rationale = suggestion.Rationale ?? string.Empty,
                ProposedChange = suggestion.ProposedChange ?? string.Empty,
                Status = suggestion.Status,
                UserSteeringInput = suggestion.UserSteeringInput
            })
            .ToList()
            .AsReadOnly();

        return new Document
        {
            Id = normalizedDocumentId,
            UserId = targetUserId,
            Filename = string.IsNullOrWhiteSpace(sourceDocument.Filename)
                ? $"{normalizedDocumentId}.docx"
                : sourceDocument.Filename,
            Source = sourceDocument.Source,
            Title = sourceDocument.Title ?? string.Empty,
            Status = sourceDocument.Status,
            CreatedAt = importedAt,
            UpdatedAt = importedAt,
            Paragraphs = normalizedParagraphs,
            Suggestions = normalizedSuggestions
        };
    }

    private static string NormalizeUserId(string? userId)
    {
        return string.IsNullOrWhiteSpace(userId) ? "_anonymous" : userId;
    }

    private static string? CombineGuidance(string? instructions, string? tone)
    {
        if (string.IsNullOrWhiteSpace(instructions) && string.IsNullOrWhiteSpace(tone))
        {
            return null;
        }

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(instructions))
        {
            parts.Add(instructions);
        }

        if (!string.IsNullOrWhiteSpace(tone))
        {
            parts.Add($"Desired tone: {tone}");
        }

        return string.Join(" ", parts);
    }

    private static IReadOnlyList<Paragraph> SplitIntoParagraphs(string content)
    {
        return content
            .Split(["\n\n"], StringSplitOptions.None)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(text => new Paragraph
            {
                Id = Guid.NewGuid().ToString("N"),
                Text = text
            })
            .ToList()
            .AsReadOnly();
    }
}
