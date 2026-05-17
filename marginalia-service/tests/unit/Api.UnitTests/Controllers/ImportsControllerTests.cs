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
public sealed class ImportsControllerTests
{
    private IImportService _importService = null!;
    private IImportExportJobRepository _jobRepository = null!;
    private ILogger<ImportsController> _logger = null!;
    private ImportsController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _importService = Substitute.For<IImportService>();
        _jobRepository = Substitute.For<IImportExportJobRepository>();
        _logger = Substitute.For<ILogger<ImportsController>>();

        _controller = new ImportsController(
            _importService,
            _jobRepository,
            _logger,
            isMultiUserModeProvider: () => false);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [TestMethod]
    public async Task StartImport_ReturnsAcceptedWithJobId()
    {
        using var archiveStream = new MemoryStream("archive"u8.ToArray());
        IFormFile file = new FormFile(archiveStream, 0, archiveStream.Length, "file", "manuscripts.zip");

        _importService.StartImportAsync(
                "_anonymous",
                false,
                Arg.Any<string>(),
                true,
                Arg.Any<CancellationToken>())
            .Returns("job-1");

        var result = await _controller.StartImport(file, overwrite: true, CancellationToken.None);

        var acceptedResult = result.Result.Should().BeOfType<AcceptedResult>().Subject;
        acceptedResult.Value.Should().BeEquivalentTo(new { jobId = "job-1" });
    }

    [TestMethod]
    public async Task StartImport_InMultiUserMode_WithNoUserIdHeader_ReturnsUnauthorized()
    {
        _controller = new ImportsController(
            _importService,
            _jobRepository,
            _logger,
            isMultiUserModeProvider: () => true)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        using var archiveStream = new MemoryStream("archive"u8.ToArray());
        IFormFile file = new FormFile(archiveStream, 0, archiveStream.Length, "file", "manuscripts.zip");

        var result = await _controller.StartImport(file, overwrite: false, CancellationToken.None);

        var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().BeEquivalentTo(new { error = "X-User-Id header is required in multi-user mode." });
    }

    [TestMethod]
    public async Task StartImport_ReturnsBadRequest_ForNonZipFiles()
    {
        using var archiveStream = new MemoryStream("archive"u8.ToArray());
        IFormFile file = new FormFile(archiveStream, 0, archiveStream.Length, "file", "manuscripts.txt");

        var result = await _controller.StartImport(file, overwrite: false, CancellationToken.None);

        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeEquivalentTo(new { error = "Only .zip files are supported for manuscript import." });
    }

    [TestMethod]
    public async Task GetImportJob_ReturnsNotFound_WhenJobMissing()
    {
        _jobRepository.GetByIdAsync("_anonymous", "missing", Arg.Any<CancellationToken>())
            .Returns((ImportExportJob?)null);

        var result = await _controller.GetImportJob("missing", CancellationToken.None);

        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeEquivalentTo(new { error = "Import job 'missing' not found." });
    }

    [TestMethod]
    public async Task GetImportJob_ReturnsJob_WhenFound()
    {
        var job = new ImportExportJob
        {
            Id = "job-1",
            UserId = "_anonymous",
            JobType = ImportExportJobType.Import,
            Status = JobStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
            ProgressPercentage = 100,
            Counts = new ImportExportCounts
            {
                DocumentsImported = 2,
                DocumentsSkipped = 0,
                Failed = 0
            }
        };

        _jobRepository.GetByIdAsync("_anonymous", "job-1", Arg.Any<CancellationToken>())
            .Returns(job);

        var result = await _controller.GetImportJob("job-1", CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ImportExportJobResponse>().Subject;
        response.Id.Should().Be("job-1");
        response.Status.Should().Be(JobStatus.Completed);
        response.Counts?.DocumentsImported.Should().Be(2);
    }
}
