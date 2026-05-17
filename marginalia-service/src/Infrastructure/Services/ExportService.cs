using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;

namespace Marginalia.Infrastructure.Services;

public sealed class ExportService : IExportService
{
    private readonly IImportExportJobRepository _jobRepository;
    private readonly IImportExportJobQueue _jobQueue;

    public ExportService(IImportExportJobRepository jobRepository, IImportExportJobQueue jobQueue)
    {
        _jobRepository = jobRepository;
        _jobQueue = jobQueue;
    }

    public async Task<string> StartExportAsync(string userId, bool multiUserMode, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var job = new ImportExportJob
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            JobType = ImportExportJobType.Export,
            Status = JobStatus.Queued,
            CreatedAt = now,
            ProgressPercentage = 0,
            CurrentStage = "Queued",
            OverwriteExisting = false,
            IsMultiUserMode = multiUserMode
        };

        await _jobRepository.CreateAsync(job, cancellationToken);
        await _jobQueue.EnqueueAsync(new ImportExportJobQueueItem
        {
            JobId = job.Id,
            UserId = userId,
            JobType = ImportExportJobType.Export
        }, cancellationToken);

        return job.Id;
    }
}
