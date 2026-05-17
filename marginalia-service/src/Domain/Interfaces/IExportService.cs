namespace Marginalia.Domain.Interfaces;

public interface IExportService
{
    Task<string> StartExportAsync(string userId, bool multiUserMode, CancellationToken cancellationToken = default);
}
