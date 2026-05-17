using FluentAssertions;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using Marginalia.Infrastructure.Services;
using NSubstitute;

namespace Marginalia.Tests.Unit.Infrastructure.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class ExportServiceTests
{
    private IImportExportJobRepository _jobRepository = null!;
    private IImportExportJobQueue _jobQueue = null!;
    private ExportService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _jobRepository = Substitute.For<IImportExportJobRepository>();
        _jobQueue = Substitute.For<IImportExportJobQueue>();
        _service = new ExportService(_jobRepository, _jobQueue);
    }

    [TestMethod]
    public async Task StartExportAsync_CreatesQueuedJobAndEnqueues()
    {
        var jobId = await _service.StartExportAsync("user-a", multiUserMode: true, CancellationToken.None);

        jobId.Should().NotBeNullOrWhiteSpace();

        await _jobRepository.Received(1).CreateAsync(
            Arg.Is<ImportExportJob>(job =>
                job.Id == jobId &&
                job.UserId == "user-a" &&
                job.JobType == ImportExportJobType.Export &&
                job.Status == JobStatus.Queued &&
                job.IsMultiUserMode &&
                !job.OverwriteExisting),
            Arg.Any<CancellationToken>());

        await _jobQueue.Received(1).EnqueueAsync(
            Arg.Is<ImportExportJobQueueItem>(item =>
                item.JobId == jobId &&
                item.UserId == "user-a" &&
                item.JobType == ImportExportJobType.Export),
            Arg.Any<CancellationToken>());
    }
}
