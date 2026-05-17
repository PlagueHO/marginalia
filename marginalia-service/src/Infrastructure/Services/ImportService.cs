using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;

namespace Marginalia.Infrastructure.Services;

public sealed class ImportService : IImportService
{
    private readonly IImportExportJobRepository _jobRepository;
    private readonly IImportExportJobQueue _jobQueue;

    public ImportService(IImportExportJobRepository jobRepository, IImportExportJobQueue jobQueue)
    {
        _jobRepository = jobRepository;
        _jobQueue = jobQueue;
    }

    public async Task<string> StartImportAsync(
        string userId,
        bool multiUserMode,
        string sourceFilePath,
        bool overwriteExisting,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Import source archive was not found.", sourceFilePath);
        }

        var now = DateTimeOffset.UtcNow;
        var job = new ImportExportJob
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            JobType = ImportExportJobType.Import,
            Status = JobStatus.Queued,
            CreatedAt = now,
            ProgressPercentage = 0,
            CurrentStage = "Queued",
            OverwriteExisting = overwriteExisting,
            SourceFilePath = sourceFilePath,
            IsMultiUserMode = multiUserMode
        };

        await _jobRepository.CreateAsync(job, cancellationToken);
        await _jobQueue.EnqueueAsync(new ImportExportJobQueueItem
        {
            JobId = job.Id,
            UserId = userId,
            JobType = ImportExportJobType.Import
        }, cancellationToken);

        return job.Id;
    }
}
