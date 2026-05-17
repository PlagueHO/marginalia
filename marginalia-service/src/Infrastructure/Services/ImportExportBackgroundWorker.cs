using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Marginalia.Infrastructure.Services;

public sealed class ImportExportBackgroundWorker : BackgroundService
{
    private readonly IImportExportJobQueue _jobQueue;
    private readonly IImportExportJobRepository _jobRepository;
    private readonly ExportJobProcessor _exportJobProcessor;
    private readonly ImportJobProcessor _importJobProcessor;
    private readonly ILogger<ImportExportBackgroundWorker> _logger;

    public ImportExportBackgroundWorker(
        IImportExportJobQueue jobQueue,
        IImportExportJobRepository jobRepository,
        ExportJobProcessor exportJobProcessor,
        ImportJobProcessor importJobProcessor,
        ILogger<ImportExportBackgroundWorker> logger)
    {
        _jobQueue = jobQueue;
        _jobRepository = jobRepository;
        _exportJobProcessor = exportJobProcessor;
        _importJobProcessor = importJobProcessor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var queueItem = await _jobQueue.DequeueAsync(stoppingToken);
                var job = await _jobRepository.GetByIdAsync(queueItem.UserId, queueItem.JobId, stoppingToken);
                if (job is null)
                {
                    _logger.LogWarning("Queued job could not be loaded: {JobId}, UserId: {UserId}", queueItem.JobId, queueItem.UserId);
                    continue;
                }

                if (queueItem.JobType == ImportExportJobType.Export)
                {
                    await _exportJobProcessor.ProcessAsync(job, stoppingToken);
                }
                else
                {
                    await _importJobProcessor.ProcessAsync(job, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in import/export background worker loop.");
            }
        }
    }
}
