using FluentAssertions;
using System.IO.Compression;
using System.Text.Json;
using Marginalia.Domain.Interfaces;
using Marginalia.Domain.Models;
using Marginalia.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Marginalia.Infrastructure.UnitTests.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class ImportJobProcessorTests
{
    private IDocumentRepository _documentRepository = null!;
    private IImportExportJobRepository _jobRepository = null!;
    private ILogger<ImportJobProcessor> _logger = null!;
    private ImportJobProcessor _processor = null!;

    [TestInitialize]
    public void Setup()
    {
        _documentRepository = Substitute.For<IDocumentRepository>();
        _jobRepository = Substitute.For<IImportExportJobRepository>();
        _logger = Substitute.For<ILogger<ImportJobProcessor>>();
        _processor = new ImportJobProcessor(_documentRepository, _jobRepository, _logger);
    }

    [TestMethod]
    public async Task ProcessAsync_WhenSourceFileMissing_MarksJobFailed()
    {
        var job = CreateImportJob(sourceFilePath: Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.zip"));

        await _processor.ProcessAsync(job, CancellationToken.None);

        await _jobRepository.Received(1).UpdateAsync(
            Arg.Is<ImportExportJob>(updated =>
                updated.Status == JobStatus.Failed &&
                updated.CurrentStage == "Failed" &&
                updated.ErrorMessage == "Import source archive was not found."),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ProcessAsync_WhenArchiveMalformed_MarksJobFailedAndDeletesSourceFile()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"malformed-{Guid.NewGuid():N}.zip");
        await File.WriteAllTextAsync(sourcePath, "not-a-valid-zip", CancellationToken.None);

        var job = CreateImportJob(sourcePath);

        await _processor.ProcessAsync(job, CancellationToken.None);

        await _jobRepository.Received().UpdateAsync(
            Arg.Is<ImportExportJob>(updated =>
                updated.Status == JobStatus.Failed &&
                updated.ErrorMessage == "Archive must contain exactly one manuscripts.json file with valid document data."),
            Arg.Any<CancellationToken>());

        File.Exists(sourcePath).Should().BeFalse();
    }

    [TestMethod]
    public async Task ProcessAsync_ProgressAndStagesAreMonotonic()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"valid-{Guid.NewGuid():N}.zip");
        var updates = new List<ImportExportJob>();

        _jobRepository
            .When(repo => repo.UpdateAsync(Arg.Any<ImportExportJob>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => updates.Add(callInfo.Arg<ImportExportJob>()));

        var documents = new List<Document>
        {
            new()
            {
                Id = "source-1",
                UserId = "seed",
                Filename = "seed-1.docx",
                Source = DocumentSource.Local,
                Paragraphs = [new Paragraph { Id = "p1", Text = "alpha" }],
                Suggestions = []
            },
            new()
            {
                Id = "source-2",
                UserId = "seed",
                Filename = "seed-2.docx",
                Source = DocumentSource.Local,
                Paragraphs = [new Paragraph { Id = "p2", Text = "beta" }],
                Suggestions = []
            }
        };

        await BuildArchiveAsync(sourcePath, documents, CancellationToken.None);

        try
        {
            var job = CreateImportJob(sourcePath);
            await _processor.ProcessAsync(job, CancellationToken.None);

            updates.Should().NotBeEmpty();
            updates.Select(update => update.ProgressPercentage).Should().BeInAscendingOrder();

            updates.Select(update => update.CurrentStage).Should().ContainInOrder("Reading archive", "Persisting documents", "Completed");

            var completed = updates.Last();
            completed.Status.Should().Be(JobStatus.Completed);
            completed.ProgressPercentage.Should().Be(100);
            completed.TotalItems.Should().Be(2);
            completed.ProcessedItems.Should().Be(2);
        }
        finally
        {
            if (File.Exists(sourcePath))
            {
                File.Delete(sourcePath);
            }
        }
    }

    private static async Task BuildArchiveAsync(string outputPath, IReadOnlyList<Document> documents, CancellationToken cancellationToken)
    {
        await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create, leaveOpen: false);
        var manuscriptsEntry = zipArchive.CreateEntry("manuscripts.json", CompressionLevel.NoCompression);
        await using var entryStream = manuscriptsEntry.Open();
        await JsonSerializer.SerializeAsync(entryStream, documents, cancellationToken: cancellationToken);
    }

    private static ImportExportJob CreateImportJob(string sourceFilePath)
    {
        return new ImportExportJob
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = "user-a",
            JobType = ImportExportJobType.Import,
            Status = JobStatus.Queued,
            CreatedAt = DateTimeOffset.UtcNow,
            ProgressPercentage = 0,
            CurrentStage = "Queued",
            SourceFilePath = sourceFilePath
        };
    }
}
