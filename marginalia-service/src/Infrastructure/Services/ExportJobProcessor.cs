using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Marginalia.Infrastructure.Services;

public sealed class ExportJobProcessor
{
    private static readonly JsonSerializerOptions ArchiveSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IDocumentRepository _documentRepository;
    private readonly IImportExportJobRepository _jobRepository;
    private readonly ILogger<ExportJobProcessor> _logger;

    public ExportJobProcessor(
        IDocumentRepository documentRepository,
        IImportExportJobRepository jobRepository,
        ILogger<ExportJobProcessor> logger)
    {
        _documentRepository = documentRepository;
        _jobRepository = jobRepository;
        _logger = logger;
    }

    public async Task ProcessAsync(ImportExportJob job, CancellationToken cancellationToken)
    {
        var runningJob = job with
        {
            Status = JobStatus.Running,
            StartedAt = DateTimeOffset.UtcNow,
            ProgressPercentage = 15,
            CurrentStage = "Collecting documents"
        };
        await _jobRepository.UpdateAsync(runningJob, cancellationToken);

        try
        {
            var documents = job.IsMultiUserMode
                ? await _documentRepository.GetByUserAsync(job.UserId, cancellationToken)
                : await _documentRepository.GetAllAsync(cancellationToken);

            var archivingJob = runningJob with
            {
                ProgressPercentage = 70,
                CurrentStage = "Creating archive",
                TotalItems = documents.Count,
                ProcessedItems = documents.Count
            };
            await _jobRepository.UpdateAsync(archivingJob, cancellationToken);

            var exportPath = Path.Combine(Path.GetTempPath(), $"marginalia-export-{job.Id}.zip");
            await WriteArchiveAsync(exportPath, documents, cancellationToken);

            var completedJob = archivingJob with
            {
                Status = JobStatus.Completed,
                CompletedAt = DateTimeOffset.UtcNow,
                ProgressPercentage = 100,
                CurrentStage = "Completed",
                TotalItems = documents.Count,
                ProcessedItems = documents.Count,
                ResultFilePath = exportPath,
                ErrorMessage = null
            };

            await _jobRepository.UpdateAsync(completedJob, cancellationToken);
            _logger.LogInformation("Export job completed: {JobId}, UserId: {UserId}, DocumentCount: {DocumentCount}", job.Id, job.UserId, documents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export job failed: {JobId}, UserId: {UserId}", job.Id, job.UserId);

            var failedJob = runningJob with
            {
                Status = JobStatus.Failed,
                CompletedAt = DateTimeOffset.UtcNow,
                ProgressPercentage = 100,
                CurrentStage = "Failed",
                ErrorMessage = ex.Message
            };

            await _jobRepository.UpdateAsync(failedJob, cancellationToken);
        }
    }

    private static async Task WriteArchiveAsync(
        string outputPath,
        IReadOnlyList<Document> documents,
        CancellationToken cancellationToken)
    {
        await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create, leaveOpen: false);

        var manuscriptsEntry = zipArchive.CreateEntry("manuscripts.json", CompressionLevel.Optimal);
        await using var entryStream = manuscriptsEntry.Open();
        await JsonSerializer.SerializeAsync(entryStream, documents, ArchiveSerializerOptions, cancellationToken);
    }
}
