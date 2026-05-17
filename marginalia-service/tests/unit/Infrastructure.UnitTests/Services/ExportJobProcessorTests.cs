using FluentAssertions;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using Marginalia.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Marginalia.Infrastructure.UnitTests.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class ExportJobProcessorTests
{
    private IDocumentRepository _documentRepository = null!;
    private IImportExportJobRepository _jobRepository = null!;
    private ILogger<ExportJobProcessor> _logger = null!;
    private ExportJobProcessor _processor = null!;

    [TestInitialize]
    public void Setup()
    {
        _documentRepository = Substitute.For<IDocumentRepository>();
        _jobRepository = Substitute.For<IImportExportJobRepository>();
        _logger = Substitute.For<ILogger<ExportJobProcessor>>();
        _processor = new ExportJobProcessor(_documentRepository, _jobRepository, _logger);
    }

    [TestMethod]
    public async Task ProcessAsync_WhenDocumentQueryThrows_MarksJobFailed()
    {
        var job = CreateExportJob();

        _documentRepository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<Document>>(new InvalidOperationException("query failed")));

        await _processor.ProcessAsync(job, CancellationToken.None);

        await _jobRepository.Received().UpdateAsync(
            Arg.Is<ImportExportJob>(updated =>
                updated.Status == JobStatus.Failed &&
                updated.CurrentStage == "Failed" &&
                updated.ErrorMessage == "query failed"),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ProcessAsync_UpdatesArchiveStageBeforeCompleting()
    {
        var job = CreateExportJob();
        var expectedArchivePath = Path.Combine(Path.GetTempPath(), $"marginalia-export-{job.Id}.zip");
        _documentRepository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([
                new Document
                {
                    Id = "doc-1",
                    UserId = "user-a",
                    Filename = "doc-1.docx",
                    Source = DocumentSource.Local,
                    Paragraphs = []
                }
            ]);

        await _processor.ProcessAsync(job, CancellationToken.None);

        await _jobRepository.Received().UpdateAsync(
            Arg.Is<ImportExportJob>(updated =>
                updated.Status == JobStatus.Running &&
                updated.CurrentStage == "Creating archive" &&
                updated.ProgressPercentage == 70 &&
                updated.TotalItems == 1 &&
                updated.ProcessedItems == 1),
            Arg.Any<CancellationToken>());

        await _jobRepository.Received().UpdateAsync(
            Arg.Is<ImportExportJob>(updated =>
                updated.Status == JobStatus.Completed &&
                updated.CurrentStage == "Completed" &&
                updated.ProgressPercentage == 100 &&
                updated.ResultFilePath == expectedArchivePath),
            Arg.Any<CancellationToken>());

        if (File.Exists(expectedArchivePath))
        {
            File.Delete(expectedArchivePath);
        }
    }

    [TestMethod]
    public async Task ProcessAsync_ProgressAndStagesAreMonotonic()
    {
        var job = CreateExportJob();
        var expectedArchivePath = Path.Combine(Path.GetTempPath(), $"marginalia-export-{job.Id}.zip");
        var updates = new List<ImportExportJob>();

        _jobRepository
            .When(repo => repo.UpdateAsync(Arg.Any<ImportExportJob>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => updates.Add(callInfo.Arg<ImportExportJob>()));

        _documentRepository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([
                new Document { Id = "doc-1", UserId = "user-a", Filename = "doc-1.docx", Source = DocumentSource.Local, Paragraphs = [] },
                new Document { Id = "doc-2", UserId = "user-a", Filename = "doc-2.docx", Source = DocumentSource.Local, Paragraphs = [] },
                new Document { Id = "doc-3", UserId = "user-a", Filename = "doc-3.docx", Source = DocumentSource.Local, Paragraphs = [] }
            ]);

        await _processor.ProcessAsync(job, CancellationToken.None);

        updates.Should().HaveCount(3);
        updates.Select(update => update.CurrentStage).Should().Equal("Collecting documents", "Creating archive", "Completed");
        updates.Select(update => update.ProgressPercentage).Should().Equal(15, 70, 100);

        if (File.Exists(expectedArchivePath))
        {
            File.Delete(expectedArchivePath);
        }
    }

    private static ImportExportJob CreateExportJob()
    {
        return new ImportExportJob
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = "user-a",
            JobType = ImportExportJobType.Export,
            Status = JobStatus.Queued,
            CreatedAt = DateTimeOffset.UtcNow,
            ProgressPercentage = 0,
            CurrentStage = "Queued",
            IsMultiUserMode = false
        };
    }
}
