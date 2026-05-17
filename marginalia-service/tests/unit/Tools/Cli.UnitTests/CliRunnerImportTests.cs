using FluentAssertions;
using Marginalia.Tools.Cli;
using Marginalia.Tools.Cli.Models;
using NSubstitute;

namespace Marginalia.Tools.Cli.UnitTests;

[TestClass]
[TestCategory("Unit")]
public sealed class CliRunnerImportTests
{
    private IMarginaliaCliClient _client = null!;

    [TestInitialize]
    public void Setup()
    {
        _client = Substitute.For<IMarginaliaCliClient>();
    }

    [TestMethod]
    public async Task ImportPackageAsync_WhenFileNotFound_ReturnsExitCode1()
    {
        var result = await CliRunner.ImportPackageAsync(
            "nonexistent.zip",
            overwrite: false,
            _client,
            CancellationToken.None);

        result.Should().Be(1);
        await _client.DidNotReceive().StartImportAsync(
            Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ImportPackageAsync_WhenImportSucceeds_ReturnsExitCode0()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            _client.StartImportAsync(tempFile, false, Arg.Any<CancellationToken>())
                .Returns("job-1");
            _client.GetImportJobAsync("job-1", Arg.Any<CancellationToken>())
                .Returns(new JobStatusResponse { Id = "job-1", Status = "Completed" });

            var result = await CliRunner.ImportPackageAsync(tempFile, false, _client, CancellationToken.None);

            result.Should().Be(0);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [TestMethod]
    public async Task ImportPackageAsync_WhenImportFails_ReturnsExitCode1()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            _client.StartImportAsync(tempFile, false, Arg.Any<CancellationToken>())
                .Returns("job-1");
            _client.GetImportJobAsync("job-1", Arg.Any<CancellationToken>())
                .Returns(new JobStatusResponse
                {
                    Id = "job-1",
                    Status = "Failed",
                    ErrorMessage = "Something went wrong.",
                });

            var result = await CliRunner.ImportPackageAsync(tempFile, false, _client, CancellationToken.None);

            result.Should().Be(1);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [TestMethod]
    public async Task ImportPackageAsync_WhenStartImportThrows_ReturnsExitCode1()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            _client.StartImportAsync(tempFile, false, Arg.Any<CancellationToken>())
                .Returns(Task.FromException<string>(new HttpRequestException("Connection refused")));

            var result = await CliRunner.ImportPackageAsync(tempFile, false, _client, CancellationToken.None);

            result.Should().Be(1);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [TestMethod]
    public async Task ImportPackageAsync_PassesOverwriteFlagToClient()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            _client.StartImportAsync(tempFile, true, Arg.Any<CancellationToken>())
                .Returns("job-1");
            _client.GetImportJobAsync("job-1", Arg.Any<CancellationToken>())
                .Returns(new JobStatusResponse { Id = "job-1", Status = "Completed" });

            await CliRunner.ImportPackageAsync(tempFile, overwrite: true, _client, CancellationToken.None);

            await _client.Received(1).StartImportAsync(tempFile, true, Arg.Any<CancellationToken>());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
