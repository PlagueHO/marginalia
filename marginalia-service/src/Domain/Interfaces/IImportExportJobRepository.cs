using Marginalia.Domain.Models;

namespace Marginalia.Domain.Interfaces;

public interface IImportExportJobRepository
{
    Task CreateAsync(ImportExportJob job, CancellationToken cancellationToken = default);

    Task<ImportExportJob?> GetByIdAsync(string userId, string jobId, CancellationToken cancellationToken = default);

    Task UpdateAsync(ImportExportJob job, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ImportExportJob>> ListActiveByUserAsync(string userId, CancellationToken cancellationToken = default);
}
