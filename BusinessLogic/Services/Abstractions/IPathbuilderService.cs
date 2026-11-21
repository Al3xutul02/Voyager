using BusinessLogic.Dtos.Pathbuilder;

namespace BusinessLogic.Services.Abstractions;

/// <summary>
/// Service abstraction for retrieving and parsing Pathfinder 2e characters from Pathbuilder 2e.
/// </summary>
public interface IPathbuilderService
{
    /// <summary>
    /// Fetches character build information from Pathbuilder 2e using an export ID.
    /// </summary>
    /// <param name="pathbuilderId">The Pathbuilder export ID (e.g. "123456" or URL containing it).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A populated <see cref="PathbuilderCharacterDto"/> if found and valid; otherwise <c>null</c>.</returns>
    public Task<PathbuilderCharacterDto?> FetchCharacterAsync(
        string pathbuilderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sanitizes an input string to extract the raw Pathbuilder export ID.
    /// </summary>
    /// <param name="input">The raw user input string.</param>
    /// <returns>The sanitized export ID.</returns>
    public string SanitizeExportId(string input);
}
