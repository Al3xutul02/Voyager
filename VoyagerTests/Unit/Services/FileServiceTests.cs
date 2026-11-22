using System.Net;
using System.Text;
using BusinessLogic.Services;

namespace VoyagerTests.Services;

/// <summary>
/// Unit tests for <see cref="FileService"/>.
/// Validates disk operations, attachment downloading, directory management, and directory traversal security.
/// </summary>
[TestClass]
public class FileServiceTests
{
    private string _tempDir = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "VoyagerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [TestMethod]
    public async Task SaveFileAsync_WritesStreamToDiskAndReturnsRelativePath()
    {
        using var client = new HttpClient();
        var service = new FileService(client, _tempDir);

        var content = "test file content";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var relativePath = await service.SaveFileAsync(stream, "Characters/123/1", "token.png");

        Assert.AreEqual("Characters/123/1/token.png", relativePath);
        var fullPath = service.GetFullPath(relativePath);
        Assert.IsTrue(File.Exists(fullPath));

        var readText = await File.ReadAllTextAsync(fullPath);
        Assert.AreEqual(content, readText);
    }

    [TestMethod]
    public async Task SaveAttachmentAsync_DownloadsAndSavesFile()
    {
        var expectedBytes = new byte[] { 1, 2, 3, 4, 5 };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(expectedBytes)
        });

        using var client = new HttpClient(handler);
        var service = new FileService(client, _tempDir);

        var relativePath = await service.SaveAttachmentAsync("https://cdn.discordapp.com/attachments/1/2/token.png", "Characters/456/2", "token.png");

        Assert.AreEqual("Characters/456/2/token.png", relativePath);
        var fullPath = service.GetFullPath(relativePath);
        Assert.IsTrue(File.Exists(fullPath));

        var savedBytes = await File.ReadAllBytesAsync(fullPath);
        CollectionAssert.AreEqual(expectedBytes, savedBytes);
    }

    [TestMethod]
    public async Task OpenRead_ReturnsReadableStream_WhenFileExists()
    {
        using var client = new HttpClient();
        var service = new FileService(client, _tempDir);

        var content = "actor image bytes";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var relativePath = await service.SaveFileAsync(stream, "Characters/123/1", "actor.png");

        using var readStream = service.OpenRead(relativePath);

        Assert.IsNotNull(readStream);
        using var reader = new StreamReader(readStream!);
        var text = await reader.ReadToEndAsync();
        Assert.AreEqual(content, text);
    }

    [TestMethod]
    public void OpenRead_ReturnsNull_WhenFileDoesNotExist()
    {
        using var client = new HttpClient();
        var service = new FileService(client, _tempDir);

        var stream = service.OpenRead("Characters/999/nonexistent.png");

        Assert.IsNull(stream);
    }

    [TestMethod]
    public async Task DeleteFile_RemovesFile_WhenFileExists()
    {
        using var client = new HttpClient();
        var service = new FileService(client, _tempDir);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("sample"));
        var relativePath = await service.SaveFileAsync(stream, "Characters/123/1", "delete_me.png");

        var deleted = service.DeleteFile(relativePath);

        Assert.IsTrue(deleted);
        Assert.IsFalse(File.Exists(service.GetFullPath(relativePath)));
    }

    [TestMethod]
    public async Task DeleteDirectory_RemovesDirectoryAndContents()
    {
        using var client = new HttpClient();
        var service = new FileService(client, _tempDir);

        using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("token"));
        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes("actor"));
        await service.SaveFileAsync(stream1, "Characters/123/1", "token.png");
        await service.SaveFileAsync(stream2, "Characters/123/1", "actor.png");

        var deleted = service.DeleteDirectory("Characters/123/1");

        Assert.IsTrue(deleted);
        var fullDirPath = Path.Combine(_tempDir, "Characters/123/1");
        Assert.IsFalse(Directory.Exists(fullDirPath));
    }

    [TestMethod]
    public async Task SaveFileAsync_ThrowsUnauthorizedAccessException_OnDirectoryTraversal()
    {
        using var client = new HttpClient();
        var service = new FileService(client, _tempDir);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("evil"));

        bool threw = false;
        try
        {
            await service.SaveFileAsync(stream, "../../escaped", "hack.png");
        }
        catch (UnauthorizedAccessException)
        {
            threw = true;
        }

        Assert.IsTrue(threw, "Expected UnauthorizedAccessException on directory traversal attempt.");
    }

    private class MockHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }
}
