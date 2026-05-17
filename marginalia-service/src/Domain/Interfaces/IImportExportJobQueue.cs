using Marginalia.Domain.Models;

namespace Marginalia.Domain.Interfaces;

public interface IImportExportJobQueue
{
    ValueTask EnqueueAsync(ImportExportJobQueueItem item, CancellationToken cancellationToken = default);

    ValueTask<ImportExportJobQueueItem> DequeueAsync(CancellationToken cancellationToken = default);
}
