using Marginalia.Tools.Cli.Models;

namespace Marginalia.Tools.Cli;

internal interface IMarginaliaCliClient
{
    Task<string> StartImportAsync(string filePath, bool overwrite, CancellationToken cancellationToken);
    Task<JobStatusResponse> GetImportJobAsync(string jobId, CancellationToken cancellationToken);
    Task<string> StartExportAsync(CancellationToken cancellationToken);
    Task<JobStatusResponse> GetExportJobAsync(string jobId, CancellationToken cancellationToken);
    Task DownloadExportAsync(string jobId, string outputPath, CancellationToken cancellationToken);
}
