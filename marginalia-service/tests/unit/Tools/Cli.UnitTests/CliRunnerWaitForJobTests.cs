using FluentAssertions;
using Marginalia.Tools.Cli;
using Marginalia.Tools.Cli.Models;

namespace Marginalia.Tools.Cli.UnitTests;

[TestClass]
[TestCategory("Unit")]
public sealed class CliRunnerWaitForJobTests
{
    [TestMethod]
    public async Task WaitForJobAsync_WhenJobAlreadyCompleted_ReturnsExitCode0()
    {
        var result = await CliRunner.WaitForJobAsync(
            () => Task.FromResult(new JobStatusResponse { Id = "job-1", Status = "Completed" }),
            "job-1",
            "export",
            CancellationToken.None);

        result.Should().Be(0);
    }

    [TestMethod]
    public async Task WaitForJobAsync_WhenJobFailed_ReturnsExitCode1()
    {
        var result = await CliRunner.WaitForJobAsync(
            () => Task.FromResult(new JobStatusResponse
            {
                Id = "job-1",
                Status = "Failed",
                ErrorMessage = "An error occurred.",
            }),
            "job-1",
            "import",
            CancellationToken.None);

        result.Should().Be(1);
    }

    [TestMethod]
    public async Task WaitForJobAsync_WhenJobCancelled_ReturnsExitCode1()
    {
        var result = await CliRunner.WaitForJobAsync(
            () => Task.FromResult(new JobStatusResponse { Id = "job-1", Status = "Cancelled" }),
            "job-1",
            "export",
            CancellationToken.None);

        result.Should().Be(1);
    }

    [TestMethod]
    public async Task WaitForJobAsync_WhenCancellationTokenAlreadyCancelled_ReturnsExitCode1WithoutCallingGetJob()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var getJobCalled = false;
        var result = await CliRunner.WaitForJobAsync(
            () =>
            {
                getJobCalled = true;
                return Task.FromResult(new JobStatusResponse { Id = "job-1", Status = "Running" });
            },
            "job-1",
            "export",
            cts.Token);

        result.Should().Be(1);
        getJobCalled.Should().BeFalse();
    }
}
