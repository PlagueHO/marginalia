using Marginalia.Api.Models;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace Marginalia.Api.Controllers;

[ApiController]
[Route("api/exports")]
public sealed class ExportsController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly IImportExportJobRepository _jobRepository;
    private readonly ILogger<ExportsController> _logger;
    private readonly Func<bool> _isMultiUserModeProvider;

    public ExportsController(
        IExportService exportService,
        IImportExportJobRepository jobRepository,
        ILogger<ExportsController> logger,
        Func<bool>? isMultiUserModeProvider = null)
    {
        _exportService = exportService;
        _jobRepository = jobRepository;
        _logger = logger;
        _isMultiUserModeProvider = isMultiUserModeProvider ?? IsMultiUserModeFromEnvironment;
    }

    [HttpPost]
    public async Task<ActionResult<object>> StartExport(CancellationToken cancellationToken)
    {
        var userId = GetUserId(Request);
        var multiUserMode = _isMultiUserModeProvider();

        if (multiUserMode && string.Equals(userId, "_anonymous", StringComparison.Ordinal))
        {
            _logger.LogWarning("Export job rejected in multi-user mode due to missing user identity.");
            return Unauthorized(new { error = "X-User-Id header is required in multi-user mode." });
        }

        var jobId = await _exportService.StartExportAsync(userId, multiUserMode, cancellationToken);
        return Accepted(new { jobId });
    }

    [HttpGet("{jobId}")]
    public async Task<ActionResult<ImportExportJobResponse>> GetExportJob(string jobId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(Request);
        var job = await _jobRepository.GetByIdAsync(userId, jobId, cancellationToken);
        if (job is null || job.JobType != ImportExportJobType.Export)
        {
            return NotFound(new { error = $"Export job '{jobId}' not found." });
        }

        return Ok(ImportExportJobResponse.FromDomain(job));
    }

    [HttpGet("{jobId}/download")]
    public async Task<IActionResult> DownloadExport(string jobId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(Request);
        var job = await _jobRepository.GetByIdAsync(userId, jobId, cancellationToken);
        if (job is null || job.JobType != ImportExportJobType.Export)
        {
            return NotFound(new { error = $"Export job '{jobId}' not found." });
        }

        if (job.Status != JobStatus.Completed || string.IsNullOrWhiteSpace(job.ResultFilePath))
        {
            return Conflict(new { error = "Export is not ready for download yet." });
        }

        if (!System.IO.File.Exists(job.ResultFilePath))
        {
            _logger.LogWarning("Export result file not found for completed job.");
            return NotFound(new { error = "Export file is no longer available." });
        }

        var resultFilePath = job.ResultFilePath;
        Response.OnCompleted(() =>
        {
            _ = Task.Run(async () =>
            {
                for (var attempt = 1; attempt <= 5; attempt++)
                {
                    try
                    {
                        if (!System.IO.File.Exists(resultFilePath))
                        {
                            return;
                        }

                        System.IO.File.Delete(resultFilePath);
                        _logger.LogInformation("Deleted export result file after download.");
                        return;
                    }
                    catch (IOException ex)
                    {
                        if (attempt == 5)
                        {
                            _logger.LogWarning(ex, "Failed to delete export result file after download.");
                            return;
                        }

                        await Task.Delay(200).ConfigureAwait(false);
                    }
                }
            });

            return Task.CompletedTask;
        });

        var stream = new FileStream(resultFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, "application/zip", $"manuscripts-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.zip");
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
