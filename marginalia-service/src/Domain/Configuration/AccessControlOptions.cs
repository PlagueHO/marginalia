namespace Marginalia.Domain.Configuration;

/// <summary>
/// Configuration for optional access code protection in single-user mode.
/// When <see cref="AccessCode"/> is set, all API requests (except health and status endpoints)
/// must include a matching X-Access-Code header. When empty or null, no access control is applied.
/// </summary>
public sealed record AccessControlOptions
{
    public const string SectionName = "AccessControl";

    /// <summary>
    /// The access code required to use the application (env: ACCESS_CODE).
    /// Leave empty to disable access code protection.
    /// </summary>
    public string? AccessCode { get; init; }
}
