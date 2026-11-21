using BusinessLogic.Services.Abstractions;

namespace BusinessLogic.Services;

/// <summary>
/// Disk-based implementation of <see cref="IFileService"/>.
/// Saves, reads, and deletes files under a designated root directory.
/// </summary>
public class FileService : IFileService
{
    private readonly HttpClient _httpClient;
    private readonly string _basePath;

    /// <inheritdoc />
    public string BasePath => _basePath;

    /// <summary>
    /// Initializes a new instance of <see cref="FileService"/>.
    /// </summary>
    /// <param name="httpClient">HTTP client used for remote attachment downloads.</param>
    /// <param name="basePath">The root directory path for storage on disk; defaults to "Storage".</param>
    public FileService(HttpClient httpClient, string? basePath = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _basePath = string.IsNullOrWhiteSpace(basePath)
            ? Path.Combine(AppContext.BaseDirectory, "Storage")
            : Path.GetFullPath(basePath);

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    /// <inheritdoc />
    public async Task<string> SaveAttachmentAsync(
        string url,
        string relativeDirectory,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await SaveFileAsync(contentStream, relativeDirectory, fileName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> SaveFileAsync(
        Stream stream,
        string relativeDirectory,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var targetDir = Path.Combine(_basePath, relativeDirectory ?? string.Empty);
        EnsureSecurePath(targetDir);

        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        var fullFilePath = Path.Combine(targetDir, fileName);
        EnsureSecurePath(fullFilePath);

        await using var fileStream = new FileStream(fullFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await stream.CopyToAsync(fileStream, cancellationToken);

        return Path.Combine(relativeDirectory ?? string.Empty, fileName).Replace('\\', '/');
    }

    /// <inheritdoc />
    public Stream? OpenRead(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var fullPath = GetFullPath(relativePath);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    /// <inheritdoc />
    public bool DeleteFile(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return true;
        }

        var fullPath = GetFullPath(relativePath);
        if (!File.Exists(fullPath))
        {
            return true;
        }

        try
        {
            File.Delete(fullPath);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool DeleteDirectory(string relativeDirectory)
    {
        if (string.IsNullOrWhiteSpace(relativeDirectory))
        {
            return true;
        }

        var fullPath = Path.Combine(_basePath, relativeDirectory);
        EnsureSecurePath(fullPath);

        if (!Directory.Exists(fullPath))
        {
            return true;
        }

        try
        {
            Directory.Delete(fullPath, recursive: true);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public string GetFullPath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        var combined = Path.Combine(_basePath, relativePath);
        EnsureSecurePath(combined);
        return combined;
    }

    private void EnsureSecurePath(string targetPath)
    {
        var fullTarget = Path.GetFullPath(targetPath);
        var fullBase = Path.GetFullPath(_basePath);

        if (!fullTarget.StartsWith(fullBase, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Attempted to access path outside designated storage directory.");
        }
    }
}
