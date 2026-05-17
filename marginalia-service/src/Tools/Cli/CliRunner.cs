using System.CommandLine;
using Marginalia.Tools.Cli.Models;

namespace Marginalia.Tools.Cli;

internal static class CliRunner
{
    internal static RootCommand BuildRootCommand(Func<string, string?, IMarginaliaCliClient> clientFactory)
    {
        var apiUrlOption = new Option<string?>("--api-url")
        {
            Description = "Marginalia API base URL. Defaults to MARGINALIA_API_URL.",
        };
        var userIdOption = new Option<string?>("--user-id")
        {
            Description = "User ID sent as X-User-Id header. Defaults to MARGINALIA_USER_ID.",
        };

        var importCommand = new Command("import", "Import data into Marginalia.");

        var importPackageCommand = new Command("package", "Import a ZIP package using POST /api/imports.");
        var importFileOption = new Option<string>("--file")
        {
            Description = "Path to a .zip export file.",
            Required = true,
        };
        var importOverwriteOption = new Option<bool>("--overwrite")
        {
            Description = "Overwrite existing documents when importing.",
        };

        importPackageCommand.Options.Add(importFileOption);
        importPackageCommand.Options.Add(importOverwriteOption);
        importPackageCommand.Options.Add(apiUrlOption);
        importPackageCommand.Options.Add(userIdOption);
        importPackageCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var file = parseResult.GetValue(importFileOption)!;
            var overwrite = parseResult.GetValue(importOverwriteOption);
            var apiUrl = ResolveApiUrl(parseResult.GetValue(apiUrlOption));
            var userId = ResolveUserId(parseResult.GetValue(userIdOption));
            var client = clientFactory(apiUrl, userId);
            return await ImportPackageAsync(file, overwrite, client, cancellationToken);
        });

        importCommand.Subcommands.Add(importPackageCommand);

        var exportCommand = new Command("export", "Export data from Marginalia.");

        var exportPackageCommand = new Command("package", "Start and download an export package via /api/exports.");
        var exportOutputOption = new Option<string>("--output")
        {
            Description = "Destination ZIP file path.",
            Required = true,
        };

        exportPackageCommand.Options.Add(exportOutputOption);
        exportPackageCommand.Options.Add(apiUrlOption);
        exportPackageCommand.Options.Add(userIdOption);
        exportPackageCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var output = parseResult.GetValue(exportOutputOption)!;
            var apiUrl = ResolveApiUrl(parseResult.GetValue(apiUrlOption));
            var userId = ResolveUserId(parseResult.GetValue(userIdOption));
            var client = clientFactory(apiUrl, userId);
            return await ExportPackageAsync(output, client, cancellationToken);
        });

        exportCommand.Subcommands.Add(exportPackageCommand);

        return new RootCommand("Marginalia tools CLI")
        {
            importCommand,
            exportCommand,
        };
    }

    internal static async Task<int> ImportPackageAsync(
        string file,
        bool overwrite,
        IMarginaliaCliClient client,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(file))
        {
            Console.Error.WriteLine($"Import package not found: {file}");
            return 1;
        }

        try
        {
            var jobId = await client.StartImportAsync(file, overwrite, cancellationToken);
            Console.WriteLine($"Started import job: {jobId}");
            return await WaitForJobAsync(
                () => client.GetImportJobAsync(jobId, cancellationToken),
                jobId,
                "import",
                cancellationToken);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Import package failed: {ex.Message}");
            return 1;
        }
    }

    internal static async Task<int> ExportPackageAsync(
        string outputPath,
        IMarginaliaCliClient client,
        CancellationToken cancellationToken)
    {
        try
        {
            var jobId = await client.StartExportAsync(cancellationToken);
            Console.WriteLine($"Started export job: {jobId}");

            var waitCode = await WaitForJobAsync(
                () => client.GetExportJobAsync(jobId, cancellationToken),
                jobId,
                "export",
                cancellationToken);

            if (waitCode != 0)
            {
                return waitCode;
            }

            await client.DownloadExportAsync(jobId, outputPath, cancellationToken);
            Console.WriteLine($"Export downloaded to: {outputPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Export package failed: {ex.Message}");
            return 1;
        }
    }

    internal static async Task<int> WaitForJobAsync(
        Func<Task<JobStatusResponse>> getJob,
        string jobId,
        string jobType,
        CancellationToken cancellationToken)
    {
        string? lastStatus = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            var job = await getJob();

            if (!string.Equals(lastStatus, job.Status, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"{jobType} job {jobId}: {job.Status} ({job.ProgressPercentage}%) {job.CurrentStage}");
                lastStatus = job.Status;
            }

            if (string.Equals(job.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if (string.Equals(job.Status, "Failed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(job.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine($"{jobType} job {jobId} ended with status {job.Status}. {job.ErrorMessage}");
                return 1;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        Console.Error.WriteLine($"{jobType} job {jobId} cancelled by user.");
        return 1;
    }

    internal static string ResolveApiUrl(string? apiUrlOverride)
    {
        var apiUrl = string.IsNullOrWhiteSpace(apiUrlOverride)
            ? Environment.GetEnvironmentVariable("MARGINALIA_API_URL")
            : apiUrlOverride;

        if (string.IsNullOrWhiteSpace(apiUrl))
        {
            throw new InvalidOperationException(
                "API URL is required. Set --api-url or MARGINALIA_API_URL.");
        }

        return apiUrl;
    }

    internal static string? ResolveUserId(string? userIdOverride)
    {
        return string.IsNullOrWhiteSpace(userIdOverride)
            ? Environment.GetEnvironmentVariable("MARGINALIA_USER_ID")
            : userIdOverride;
    }
}
