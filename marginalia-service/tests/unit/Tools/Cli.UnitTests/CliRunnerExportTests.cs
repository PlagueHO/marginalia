using FluentAssertions;
using Marginalia.Tools.Cli;
using Marginalia.Tools.Cli.Models;
using NSubstitute;

namespace Marginalia.Tools.Cli.UnitTests;

[TestClass]
[TestCategory("Unit")]
public sealed class CliRunnerExportTests
{
    private IMarginaliaCliClient _client = null!;

    [TestInitialize]
    public void Setup()
    {
        _client = Substitute.For<IMarginaliaCliClient>();
    }

    [TestMethod]
    public async Task ExportPackageAsync_WhenExportSucceeds_DownloadsAndReturnsExitCode0()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"export-{Guid.NewGuid()}.zip");
        try
        {
            _client.StartExportAsync(Arg.Any<CancellationToken>())
                .Returns("job-1");
            _client.GetExportJobAsync("job-1", Arg.Any<CancellationToken>())
                .Returns(new JobStatusResponse { Id = "job-1", Status = "Completed" });

            var result = await CliRunner.ExportPackageAsync(outputPath, _client, CancellationToken.None);

            result.Should().Be(0);
            await _client.Received(1).DownloadExportAsync("job-1", outputPath, Arg.Any<CancellationToken>());
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [TestMethod]
    public async Task ExportPackageAsync_WhenExportFails_ReturnsExitCode1AndDoesNotDownload()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"export-{Guid.NewGuid()}.zip");

        _client.StartExportAsync(Arg.Any<CancellationToken>())
            .Returns("job-1");
        _client.GetExportJobAsync("job-1", Arg.Any<CancellationToken>())
            .Returns(new JobStatusResponse { Id = "job-1", Status = "Failed", ErrorMessage = "Export error." });

        var result = await CliRunner.ExportPackageAsync(outputPath, _client, CancellationToken.None);

        result.Should().Be(1);
        await _client.DidNotReceive().DownloadExportAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ExportPackageAsync_WhenExportCancelled_ReturnsExitCode1AndDoesNotDownload()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"export-{Guid.NewGuid()}.zip");

        _client.StartExportAsync(Arg.Any<CancellationToken>())
            .Returns("job-1");
        _client.GetExportJobAsync("job-1", Arg.Any<CancellationToken>())
            .Returns(new JobStatusResponse { Id = "job-1", Status = "Cancelled" });

        var result = await CliRunner.ExportPackageAsync(outputPath, _client, CancellationToken.None);

        result.Should().Be(1);
        await _client.DidNotReceive().DownloadExportAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ExportPackageAsync_WhenStartExportThrows_ReturnsExitCode1()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"export-{Guid.NewGuid()}.zip");

        _client.StartExportAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new HttpRequestException("Connection refused")));

        var result = await CliRunner.ExportPackageAsync(outputPath, _client, CancellationToken.None);

        result.Should().Be(1);
    }
}
