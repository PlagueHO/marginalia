using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Marginalia.Infrastructure.Services;

public sealed class ImportJobProcessor
{
    private const long MaxArchiveSizeBytes = 52_428_800;

    private static readonly JsonSerializerOptions ArchiveSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IDocumentRepository _documentRepository;
    private readonly IImportExportJobRepository _jobRepository;
    private readonly ILogger<ImportJobProcessor> _logger;

    public ImportJobProcessor(
        IDocumentRepository documentRepository,
        IImportExportJobRepository jobRepository,
        ILogger<ImportJobProcessor> logger)
    {
        _documentRepository = documentRepository;
        _jobRepository = jobRepository;
        _logger = logger;
    }

    public async Task ProcessAsync(ImportExportJob job, CancellationToken cancellationToken)
    {
        var sourceFilePath = job.SourceFilePath;
        if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
        {
            await MarkFailedAsync(job, "Import source archive was not found.", cancellationToken);
            return;
        }

        var runningJob = job with
        {
            Status = JobStatus.Running,
            StartedAt = DateTimeOffset.UtcNow,
            ProgressPercentage = 15,
            CurrentStage = "Reading archive"
        };

        await _jobRepository.UpdateAsync(runningJob, cancellationToken);

        try
        {
            await using var archiveStream = File.OpenRead(sourceFilePath);
            var sourceDocuments = await ReadArchiveAsync(archiveStream, cancellationToken);
            if (sourceDocuments is null)
            {
                await MarkFailedAsync(runningJob, "Archive must contain exactly one manuscripts.json file with valid document data.", cancellationToken);
                return;
            }

            var persistingJob = runningJob with
            {
                ProgressPercentage = 35,
                CurrentStage = "Persisting documents",
                TotalItems = sourceDocuments.Count,
                ProcessedItems = 0
            };
            await _jobRepository.UpdateAsync(persistingJob, cancellationToken);

            var importedAt = DateTimeOffset.UtcNow;
            var importedCount = 0;
            var failedCount = 0;
            var processedCount = 0;

            foreach (var sourceDocument in sourceDocuments)
            {
                try
                {
                    var normalizedDocument = NormalizeImportedDocument(sourceDocument, job.UserId, importedAt, job.OverwriteExisting);
                    await _documentRepository.SaveAsync(normalizedDocument, cancellationToken);
                    importedCount++;
                }
                catch (Exception ex)
                {
                    failedCount++;
                    _logger.LogWarning(ex, "Import job failed to save one document: {JobId}, UserId: {UserId}", job.Id, job.UserId);
                }

                processedCount++;
                var percentage = sourceDocuments.Count == 0
                    ? 90
                    : 35 + (processedCount * 55 / sourceDocuments.Count);

                persistingJob = persistingJob with
                {
                    ProgressPercentage = percentage,
                    ProcessedItems = processedCount
                };
                await _jobRepository.UpdateAsync(persistingJob, cancellationToken);
            }

            var completedJob = persistingJob with
            {
                Status = JobStatus.Completed,
                CompletedAt = DateTimeOffset.UtcNow,
                ProgressPercentage = 100,
                CurrentStage = "Completed",
                TotalItems = sourceDocuments.Count,
                ProcessedItems = sourceDocuments.Count,
                Counts = new ImportExportCounts
                {
                    DocumentsImported = importedCount,
                    DocumentsSkipped = 0,
                    Failed = failedCount
                },
                ErrorMessage = null
            };

            await _jobRepository.UpdateAsync(completedJob, cancellationToken);
            _logger.LogInformation("Import job completed: {JobId}, UserId: {UserId}, Imported: {ImportedCount}, Failed: {FailedCount}", job.Id, job.UserId, importedCount, failedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import job failed: {JobId}, UserId: {UserId}", job.Id, job.UserId);
            await MarkFailedAsync(runningJob, ex.Message, cancellationToken);
        }
        finally
        {
            try
            {
                File.Delete(sourceFilePath);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Unable to delete import source archive after processing: {Path}", sourceFilePath);
            }
        }
    }

    private async Task MarkFailedAsync(ImportExportJob job, string message, CancellationToken cancellationToken)
    {
        var failedJob = job with
        {
            Status = JobStatus.Failed,
            CompletedAt = DateTimeOffset.UtcNow,
            ProgressPercentage = 100,
            CurrentStage = "Failed",
            ErrorMessage = message
        };

        await _jobRepository.UpdateAsync(failedJob, cancellationToken);
    }

    private static async Task<IReadOnlyList<Document>?> ReadArchiveAsync(
        Stream archiveStream,
        CancellationToken cancellationToken)
    {
        try
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
            return await JsonSerializer.DeserializeAsync<List<Document>>(
                entryStream,
                ArchiveSerializerOptions,
                cancellationToken);
        }
        catch (InvalidDataException)
        {
            return null;
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Document NormalizeImportedDocument(
        Document sourceDocument,
        string currentUserId,
        DateTimeOffset importedAt,
        bool overwrite)
    {
        var targetUserId = NormalizeUserId(currentUserId);
        var normalizedDocumentId = overwrite && !string.IsNullOrWhiteSpace(sourceDocument.Id)
            ? sourceDocument.Id
            : Guid.NewGuid().ToString("N");

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
}
