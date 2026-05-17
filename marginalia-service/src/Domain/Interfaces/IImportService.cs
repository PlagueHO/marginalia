namespace Marginalia.Domain.Interfaces;

public interface IImportService
{
    Task<string> StartImportAsync(
        string userId,
        bool multiUserMode,
        string sourceFilePath,
        bool overwriteExisting,
        CancellationToken cancellationToken = default);
}
