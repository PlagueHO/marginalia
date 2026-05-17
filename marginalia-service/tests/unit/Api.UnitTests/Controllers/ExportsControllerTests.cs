using FluentAssertions;
using Marginalia.Api.Controllers;
using Marginalia.Api.Models;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Marginalia.Api.UnitTests.Controllers;

[TestClass]
[TestCategory("Unit")]
public sealed class ExportsControllerTests
{
    private IExportService _exportService = null!;
    private IImportExportJobRepository _jobRepository = null!;
    private ILogger<ExportsController> _logger = null!;
    private ExportsController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _exportService = Substitute.For<IExportService>();
        _jobRepository = Substitute.For<IImportExportJobRepository>();
        _logger = Substitute.For<ILogger<ExportsController>>();

        _controller = new ExportsController(
            _exportService,
            _jobRepository,
            _logger,
            isMultiUserModeProvider: () => false);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [TestMethod]
    public async Task StartExport_ReturnsAcceptedWithJobId()
    {
        _exportService.StartExportAsync("_anonymous", false, Arg.Any<CancellationToken>())
            .Returns("job-1");

        var result = await _controller.StartExport(CancellationToken.None);

        var acceptedResult = result.Result.Should().BeOfType<AcceptedResult>().Subject;
        acceptedResult.Value.Should().BeEquivalentTo(new { jobId = "job-1" });
    }

    [TestMethod]
    public async Task StartExport_InMultiUserMode_WithNoUserIdHeader_ReturnsUnauthorized()
    {
        _controller = new ExportsController(
            _exportService,
            _jobRepository,
            _logger,
            isMultiUserModeProvider: () => true)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await _controller.StartExport(CancellationToken.None);

        var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().BeEquivalentTo(new { error = "X-User-Id header is required in multi-user mode." });
    }

    [TestMethod]
    public async Task GetExportJob_ReturnsNotFound_WhenJobIsMissing()
    {
        _jobRepository.GetByIdAsync("_anonymous", "missing", Arg.Any<CancellationToken>())
            .Returns((ImportExportJob?)null);

        var result = await _controller.GetExportJob("missing", CancellationToken.None);

        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(new { error = "Export job 'missing' not found." });
    }

    [TestMethod]
    public async Task GetExportJob_ReturnsJob_WhenFound()
    {
        var job = new ImportExportJob
        {
            Id = "job-1",
            UserId = "_anonymous",
            JobType = ImportExportJobType.Export,
            Status = JobStatus.Running,
            CreatedAt = DateTimeOffset.UtcNow,
            ProgressPercentage = 20
        };

        _jobRepository.GetByIdAsync("_anonymous", "job-1", Arg.Any<CancellationToken>())
            .Returns(job);

        var result = await _controller.GetExportJob("job-1", CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ImportExportJobResponse>().Subject;
        response.Id.Should().Be("job-1");
        response.Status.Should().Be(JobStatus.Running);
    }

    [TestMethod]
    public async Task DownloadExport_ReturnsConflict_WhenJobNotCompleted()
    {
        var job = new ImportExportJob
        {
            Id = "job-1",
            UserId = "_anonymous",
            JobType = ImportExportJobType.Export,
            Status = JobStatus.Running,
            CreatedAt = DateTimeOffset.UtcNow,
            ProgressPercentage = 50
        };

        _jobRepository.GetByIdAsync("_anonymous", "job-1", Arg.Any<CancellationToken>())
            .Returns(job);

        var result = await _controller.DownloadExport("job-1", CancellationToken.None);

        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.Value.Should().BeEquivalentTo(new { error = "Export is not ready for download yet." });
    }

    [TestMethod]
    public async Task DownloadExport_ReturnsNotFound_WhenCompletedFileIsMissing()
    {
        var job = new ImportExportJob
        {
            Id = "job-1",
            UserId = "_anonymous",
            JobType = ImportExportJobType.Export,
            Status = JobStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
            ProgressPercentage = 100,
            ResultFilePath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.zip")
        };

        _jobRepository.GetByIdAsync("_anonymous", "job-1", Arg.Any<CancellationToken>())
            .Returns(job);

        var result = await _controller.DownloadExport("job-1", CancellationToken.None);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(new { error = "Export file is no longer available." });
    }

    [TestMethod]
    public async Task DownloadExport_ReturnsFileStreamResult_WhenCompletedFileExists()
    {
        var resultFilePath = Path.Combine(Path.GetTempPath(), $"download-{Guid.NewGuid():N}.zip");
        await File.WriteAllBytesAsync(resultFilePath, [1, 2, 3], CancellationToken.None);

        try
        {
            var job = new ImportExportJob
            {
                Id = "job-1",
                UserId = "_anonymous",
                JobType = ImportExportJobType.Export,
                Status = JobStatus.Completed,
                CreatedAt = DateTimeOffset.UtcNow,
                ProgressPercentage = 100,
                ResultFilePath = resultFilePath
            };

            _jobRepository.GetByIdAsync("_anonymous", "job-1", Arg.Any<CancellationToken>())
                .Returns(job);

            var result = await _controller.DownloadExport("job-1", CancellationToken.None);

            var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
            fileResult.ContentType.Should().Be("application/zip");
            fileResult.FileDownloadName.Should().MatchRegex("^manuscripts-\\d{14}\\.zip$");

            File.Exists(resultFilePath).Should().BeTrue();
            fileResult.FileStream.Dispose();
        }
        finally
        {
            if (File.Exists(resultFilePath))
            {
                File.Delete(resultFilePath);
            }
        }
    }
}
