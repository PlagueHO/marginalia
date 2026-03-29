using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace Marginalia.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly ISuggestionService _suggestionService;
    private readonly IWordDocumentService _wordDocumentService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentRepository documentRepository,
        ISessionRepository sessionRepository,
        ISuggestionService suggestionService,
        IWordDocumentService wordDocumentService,
        ILogger<DocumentsController> logger)
    {
        _documentRepository = documentRepository;
        _sessionRepository = sessionRepository;
        _suggestionService = suggestionService;
        _wordDocumentService = wordDocumentService;
        _logger = logger;
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
            .Select(d => new DocumentSummary
            {
                Id = d.Id,
                Title = string.IsNullOrEmpty(d.Title) ? d.Filename : d.Title,
                Filename = d.Filename,
                Source = d.Source,
                Status = d.Suggestions.Count > 0 ? DocumentStatus.Analyzed : d.Status,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                SuggestionCount = d.Suggestions.Count
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

        var document = new Document
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            Filename = request.Filename ?? "pasted-text.txt",
            Source = DocumentSource.Local,
            Content = request.Content,
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

        _logger.LogInformation("Analysis requested for document: {DocumentId}, ContentLength: {ContentLength}, UserId: {UserId}", id, document.Content.Length, userId);

        var userGuidance = CombineGuidance(request?.UserInstructions, request?.ToneGuidance);

        var suggestions = await _suggestionService.AnalyzeAsync(
            document.Id,
            document.Content,
            userGuidance,
            cancellationToken);

        // Merge new suggestions with existing ones
        var updatedSuggestions = document.Suggestions.Concat(suggestions).ToList().AsReadOnly();
        var updatedDocument = document with
        {
            Suggestions = updatedSuggestions,
            Status = DocumentStatus.Analyzed,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _documentRepository.SaveAsync(updatedDocument, cancellationToken);

        _logger.LogInformation("Analysis complete for document: {DocumentId}, SuggestionsGenerated: {Count}", id, suggestions.Count);

        return Ok(suggestions);
    }

    /// <summary>
    /// Update a suggestion's status (accept, reject, modify).
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

        var updatedSuggestions = document.Suggestions
            .Select(s => s.Id == suggestionId ? updated : s)
            .ToList()
            .AsReadOnly();

        var updatedDocument = document with { Suggestions = updatedSuggestions };
        await _documentRepository.SaveAsync(updatedDocument, cancellationToken);

        return Ok(updated);
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
}
