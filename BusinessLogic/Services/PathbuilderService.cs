using BusinessLogic.Dtos.Pathbuilder;
using BusinessLogic.Json;
using BusinessLogic.Json.Models.Pathbuilder;
using BusinessLogic.Services.Abstractions;
using Newtonsoft.Json;

namespace BusinessLogic.Services;

/// <summary>
/// Service implementation for fetching and extracting Pathfinder 2e character data
/// from Pathbuilder's export API (<c>https://pathbuilder2e.com/json.php?id={id}</c>).
/// </summary>
/// <param name="httpClient">HTTP client configured for external requests.</param>
public class PathbuilderService(HttpClient httpClient) : IPathbuilderService
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private const string BaseEndpoint = "https://pathbuilder2e.com/json.php?id=";

    /// <inheritdoc />
    public async Task<PathbuilderCharacterDto?> FetchCharacterAsync(
        string pathbuilderId,
        CancellationToken cancellationToken = default)
    {
        var sanitizedId = SanitizeExportId(pathbuilderId);
        if (string.IsNullOrWhiteSpace(sanitizedId))
        {
            return null;
        }

        try
        {
            var url = $"{BaseEndpoint}{sanitizedId}";
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseCharacterJson(json, sanitizedId);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<PathbuilderCharacterDto?> FetchCharacterFromUrlAsync(
        string jsonFileUrl,
        string? pathbuilderId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jsonFileUrl))
        {
            return null;
        }

        try
        {
            using var response = await _httpClient.GetAsync(jsonFileUrl.Trim(), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseCharacterJson(json, pathbuilderId);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public PathbuilderCharacterDto? ParseCharacterJson(string json, string? pathbuilderId = null)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var exportData = JsonConvert.DeserializeObject<PathbuilderExportResponse>(json, VoyagerJsonSettings.Default);
            if (exportData is null || !exportData.Success || exportData.Build is null)
            {
                return null;
            }

            var build = exportData.Build;
            decimal totalGold = 0m;
            if (build.Money is not null)
            {
                totalGold = build.Money.Gp
                          + (build.Money.Pp * 10m)
                          + (build.Money.Sp * 0.1m)
                          + (build.Money.Cp * 0.01m);
            }

            return new PathbuilderCharacterDto(
                Name: string.IsNullOrWhiteSpace(build.Name) ? "Unnamed Character" : build.Name.Trim(),
                Ancestry: string.IsNullOrWhiteSpace(build.Ancestry) ? "Unknown" : build.Ancestry.Trim(),
                Class: string.IsNullOrWhiteSpace(build.Class) ? "Unknown" : build.Class.Trim(),
                Level: string.IsNullOrWhiteSpace(build.Level?.ToString()) ? "1" : build.Level!.ToString()!.Trim(),
                Age: string.IsNullOrWhiteSpace(build.Age?.ToString()) ? "Unknown" : build.Age!.ToString()!.Trim(),
                Gender: string.IsNullOrWhiteSpace(build.Gender) ? "Unknown" : build.Gender.Trim(),
                Gold: totalGold,
                PathbuilderId: string.IsNullOrWhiteSpace(pathbuilderId) ? null : pathbuilderId.Trim()
            );
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public string SanitizeExportId(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var trimmed = input.Trim();

        // If the user pasted a full URL, e.g. https://pathbuilder2e.com/json.php?id=123456
        if (trimmed.Contains("id=", StringComparison.OrdinalIgnoreCase))
        {
            var parts = trimmed.Split(new[] { "id=" }, StringSplitOptions.None);
            if (parts.Length > 1)
            {
                var queryParam = parts[1].Split('&')[0].Trim();
                return queryParam;
            }
        }

        return trimmed;
    }
}
