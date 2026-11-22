namespace BusinessLogic.Services.Abstractions;

/// <summary>
/// Service abstraction for managing disk-based file operations (saving attachments,
/// reading streams, and managing character image directories).
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Gets the absolute base path where files are stored on disk.
    /// </summary>
    public string BasePath { get; }

    /// <summary>
    /// Downloads a remote file from <paramref name="url"/> and saves it locally.
    /// </summary>
    /// <param name="url">The remote download URL (e.g. Discord attachment URL).</param>
    /// <param name="relativeDirectory">The sub-directory relative to <see cref="BasePath"/>.</param>
    /// <param name="fileName">The target file name.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The relative file path where the file was saved.</returns>
    public Task<string> SaveAttachmentAsync(
        string url,
        string relativeDirectory,
        string fileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the contents of a stream to local disk.
    /// </summary>
    /// <param name="stream">The source data stream.</param>
    /// <param name="relativeDirectory">The sub-directory relative to <see cref="BasePath"/>.</param>
    /// <param name="fileName">The target file name.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The relative file path where the file was saved.</returns>
    public Task<string> SaveFileAsync(
        Stream stream,
        string relativeDirectory,
        string fileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a read stream for the file at <paramref name="relativePath"/> if it exists.
    /// </summary>
    /// <param name="relativePath">The relative path to the file.</param>
    /// <returns>A readable <see cref="Stream"/>, or <c>null</c> if the file does not exist.</returns>
    public Stream? OpenRead(string relativePath);

    /// <summary>
    /// Deletes a file at <paramref name="relativePath"/> if it exists.
    /// </summary>
    /// <param name="relativePath">The relative path to the file.</param>
    /// <returns><c>true</c> if the file was deleted or did not exist; otherwise <c>false</c>.</returns>
    public bool DeleteFile(string relativePath);

    /// <summary>
    /// Deletes a directory and all of its contents under <see cref="BasePath"/>.
    /// </summary>
    /// <param name="relativeDirectory">The relative directory path to delete.</param>
    /// <returns><c>true</c> if the directory was deleted or did not exist; otherwise <c>false</c>.</returns>
    public bool DeleteDirectory(string relativeDirectory);

    /// <summary>
    /// Resolves the absolute path on disk for a given relative path.
    /// </summary>
    /// <param name="relativePath">The relative path.</param>
    /// <returns>The absolute filesystem path.</returns>
    public string GetFullPath(string relativePath);
}
