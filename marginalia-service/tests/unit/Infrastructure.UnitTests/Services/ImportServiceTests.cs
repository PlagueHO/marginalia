using FluentAssertions;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using Marginalia.Infrastructure.Services;
using NSubstitute;

namespace Marginalia.Infrastructure.UnitTests.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class ImportServiceTests
{
    private IImportExportJobRepository _jobRepository = null!;
    private IImportExportJobQueue _jobQueue = null!;
    private ImportService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _jobRepository = Substitute.For<IImportExportJobRepository>();
        _jobQueue = Substitute.For<IImportExportJobQueue>();
        _service = new ImportService(_jobRepository, _jobQueue);
    }

    [TestMethod]
    public async Task StartImportAsync_WhenFileMissing_ThrowsFileNotFoundException()
    {
        var act = () => _service.StartImportAsync(
            "user-a",
            multiUserMode: false,
            sourceFilePath: "C:\\missing.zip",
            overwriteExisting: false,
            cancellationToken: CancellationToken.None);

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [TestMethod]
    public async Task StartImportAsync_CreatesQueuedJobAndEnqueues()
    {
        var sourcePath = Path.GetTempFileName();

        try
        {
            var jobId = await _service.StartImportAsync(
                "user-a",
                multiUserMode: true,
                sourceFilePath: sourcePath,
                overwriteExisting: true,
                cancellationToken: CancellationToken.None);

            await _jobRepository.Received(1).CreateAsync(
                Arg.Is<ImportExportJob>(job =>
                    job.Id == jobId &&
                    job.UserId == "user-a" &&
                    job.JobType == ImportExportJobType.Import &&
                    job.Status == JobStatus.Queued &&
                    job.SourceFilePath == sourcePath &&
                    job.OverwriteExisting &&
                    job.IsMultiUserMode),
                Arg.Any<CancellationToken>());

            await _jobQueue.Received(1).EnqueueAsync(
                Arg.Is<ImportExportJobQueueItem>(item =>
                    item.JobId == jobId &&
                    item.UserId == "user-a" &&
                    item.JobType == ImportExportJobType.Import),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            File.Delete(sourcePath);
        }
    }
}
