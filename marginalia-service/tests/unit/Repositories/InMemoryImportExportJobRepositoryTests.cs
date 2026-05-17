using FluentAssertions;
using Marginalia.Domain.Models;
using Marginalia.Infrastructure.Repositories;

namespace Marginalia.Tests.Unit.Repositories;

[TestClass]
[TestCategory("Unit")]
public sealed class InMemoryImportExportJobRepositoryTests
{
    private InMemoryImportExportJobRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = new InMemoryImportExportJobRepository();
    }

    [TestMethod]
    public async Task CreateAndGetByIdAsync_ReturnsJobForMatchingUser()
    {
        var job = CreateJob("job-1", "user-a", JobStatus.Queued);

        await _repository.CreateAsync(job, CancellationToken.None);
        var result = await _repository.GetByIdAsync("user-a", "job-1", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be("job-1");
    }

    [TestMethod]
    public async Task GetByIdAsync_ReturnsNullForDifferentUser()
    {
        var job = CreateJob("job-1", "user-a", JobStatus.Queued);
        await _repository.CreateAsync(job, CancellationToken.None);

        var result = await _repository.GetByIdAsync("user-b", "job-1", CancellationToken.None);

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task UpdateAsync_UpdatesExistingJob()
    {
        var job = CreateJob("job-1", "user-a", JobStatus.Queued);
        await _repository.CreateAsync(job, CancellationToken.None);

        var updated = job with
        {
            Status = JobStatus.Completed,
            CompletedAt = DateTimeOffset.UtcNow,
            ProgressPercentage = 100
        };

        await _repository.UpdateAsync(updated, CancellationToken.None);
        var result = await _repository.GetByIdAsync("user-a", "job-1", CancellationToken.None);

        result!.Status.Should().Be(JobStatus.Completed);
        result.ProgressPercentage.Should().Be(100);
    }

    [TestMethod]
    public async Task ListActiveByUserAsync_ReturnsOnlyQueuedOrRunningForUser()
    {
        await _repository.CreateAsync(CreateJob("job-queued", "user-a", JobStatus.Queued), CancellationToken.None);
        await _repository.CreateAsync(CreateJob("job-running", "user-a", JobStatus.Running), CancellationToken.None);
        await _repository.CreateAsync(CreateJob("job-completed", "user-a", JobStatus.Completed), CancellationToken.None);
        await _repository.CreateAsync(CreateJob("job-other-user", "user-b", JobStatus.Queued), CancellationToken.None);

        var activeJobs = await _repository.ListActiveByUserAsync("user-a", CancellationToken.None);

        activeJobs.Select(job => job.Id).Should().BeEquivalentTo(["job-queued", "job-running"]);
    }

    private static ImportExportJob CreateJob(string id, string userId, JobStatus status)
    {
        return new ImportExportJob
        {
            Id = id,
            UserId = userId,
            JobType = ImportExportJobType.Export,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            ProgressPercentage = 0,
            CurrentStage = "Queued"
        };
    }
}
