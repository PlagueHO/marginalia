using System.Collections.Concurrent;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;

namespace Marginalia.Infrastructure.Repositories;

public sealed class InMemoryImportExportJobRepository : IImportExportJobRepository
{
    private readonly ConcurrentDictionary<string, ImportExportJob> _jobs = new();

    public Task CreateAsync(ImportExportJob job, CancellationToken cancellationToken = default)
    {
        if (!_jobs.TryAdd(job.Id, job))
        {
            throw new InvalidOperationException($"A job with ID '{job.Id}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<ImportExportJob?> GetByIdAsync(string userId, string jobId, CancellationToken cancellationToken = default)
    {
        _jobs.TryGetValue(jobId, out var job);
        if (job is null || !string.Equals(job.UserId, userId, StringComparison.Ordinal))
        {
            return Task.FromResult<ImportExportJob?>(null);
        }

        return Task.FromResult<ImportExportJob?>(job);
    }

    public Task UpdateAsync(ImportExportJob job, CancellationToken cancellationToken = default)
    {
        _jobs.AddOrUpdate(job.Id, job, (_, _) => job);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ImportExportJob>> ListActiveByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var activeJobs = _jobs.Values
            .Where(job => string.Equals(job.UserId, userId, StringComparison.Ordinal))
            .Where(job => job.Status is JobStatus.Queued or JobStatus.Running)
            .OrderByDescending(job => job.CreatedAt)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<ImportExportJob>>(activeJobs);
    }
}
