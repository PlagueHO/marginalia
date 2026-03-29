using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace Marginalia.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SessionsController : ControllerBase
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(
        ISessionRepository sessionRepository,
        IDocumentRepository documentRepository,
        ILogger<SessionsController> logger)
    {
        _sessionRepository = sessionRepository;
        _documentRepository = documentRepository;
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
    /// Create a new editing session.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserSession>> Create(CancellationToken cancellationToken)
    {
        var userId = GetUserId(Request);
        var session = new UserSession
        {
            SessionId = Guid.NewGuid().ToString("N"),
            UserId = userId,
            DocumentIds = [],
            Timestamp = DateTimeOffset.UtcNow
        };

        await _sessionRepository.SaveAsync(session, cancellationToken);

        _logger.LogInformation("Session created: {SessionId}, UserId: {UserId}", session.SessionId, userId);

        return CreatedAtAction(nameof(GetById), new { id = session.SessionId }, session);
    }

    /// <summary>
    /// Get a session with its associated documents.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<SessionResponse>> GetById(string id, CancellationToken cancellationToken)
    {
        var userId = GetUserId(Request);
        var session = await _sessionRepository.GetByIdAsync(userId, id, cancellationToken);
        if (session is null)
        {
            _logger.LogWarning("Session not found: {SessionId}, UserId: {UserId}", id, userId);
            return NotFound(new { error = $"Session '{id}' not found." });
        }

        _logger.LogInformation("Session retrieved: {SessionId}, DocumentCount: {DocumentCount}, UserId: {UserId}", id, session.DocumentIds.Count, userId);

        var documents = new List<Document>();
        foreach (var docId in session.DocumentIds)
        {
            var doc = await _documentRepository.GetByIdAsync(userId, docId, cancellationToken);
            if (doc is not null)
            {
                documents.Add(doc);
            }
        }

        return Ok(new SessionResponse
        {
            Session = session,
            Documents = documents.AsReadOnly()
        });
    }
}

/// <summary>
/// Response DTO that includes the session and its loaded documents.
/// </summary>
public sealed record SessionResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("session")]
    public required UserSession Session { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("documents")]
    public required IReadOnlyList<Document> Documents { get; init; }
}
