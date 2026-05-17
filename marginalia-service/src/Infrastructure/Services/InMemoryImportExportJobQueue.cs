using System.Threading.Channels;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;

namespace Marginalia.Infrastructure.Services;

public sealed class InMemoryImportExportJobQueue : IImportExportJobQueue
{
    private readonly Channel<ImportExportJobQueueItem> _channel =
        Channel.CreateUnbounded<ImportExportJobQueueItem>();

    public ValueTask EnqueueAsync(ImportExportJobQueueItem item, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(item, cancellationToken);
    }

    public ValueTask<ImportExportJobQueueItem> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}
