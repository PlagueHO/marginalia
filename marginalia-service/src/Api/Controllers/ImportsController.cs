using Marginalia.Api.Models;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace Marginalia.Api.Controllers;

[ApiController]
[Route("api/imports")]
public sealed class ImportsController : ControllerBase
{
    private const long MaxArchiveSizeBytes = 52_428_800;

    private readonly IImportService _importService;
    private readonly IImportExportJobRepository _jobRepository;
    private readonly ILogger<ImportsController> _logger;
    private readonly Func<bool> _isMultiUserModeProvider;

    public ImportsController(
        IImportService importService,
        IImportExportJobRepository jobRepository,
        ILogger<ImportsController> logger,
        Func<bool>? isMultiUserModeProvider = null)
    {
        _importService = importService;
        _jobRepository = jobRepository;
        _logger = logger;
        _isMultiUserModeProvider = isMultiUserModeProvider ?? IsMultiUserModeFromEnvironment;
    }

    [HttpPost]
    [RequestSizeLimit(MaxArchiveSizeBytes)]
    public async Task<ActionResult<object>> StartImport(
        IFormFile file,
        [FromQuery] bool overwrite = false,
        CancellationToken cancellationToken = default)
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
            _logger.LogWarning("Import job rejected in multi-user mode due to missing user identity.");
            return Unauthorized(new { error = "X-User-Id header is required in multi-user mode." });
        }

        var extension = Path.GetExtension(file.FileName);
        var sourceFilePath = Path.Combine(Path.GetTempPath(), $"marginalia-import-{Guid.NewGuid():N}{extension}");

        await using (var outputStream = new FileStream(sourceFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(outputStream, cancellationToken);
        }

        var jobId = await _importService.StartImportAsync(
            userId,
            multiUserMode,
            sourceFilePath,
            overwrite,
            cancellationToken);

        return Accepted(new { jobId });
    }

    [HttpGet("{jobId}")]
    public async Task<ActionResult<ImportExportJobResponse>> GetImportJob(string jobId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(Request);
        var job = await _jobRepository.GetByIdAsync(userId, jobId, cancellationToken);
        if (job is null || job.JobType != ImportExportJobType.Import)
        {
            return NotFound(new { error = $"Import job '{jobId}' not found." });
        }

        return Ok(ImportExportJobResponse.FromDomain(job));
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
}
