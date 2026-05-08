namespace webapi.Services;

public class LocalFileService : IS3Service
{
    private readonly string _basePath;
    private readonly ILogger<LocalFileService> _logger;

    public LocalFileService(IConfiguration configuration, ILogger<LocalFileService> logger)
    {
        _basePath = configuration["LOCAL_STORAGE_PATH"] ?? "/app/uploads";
        _logger = logger;
        Directory.CreateDirectory(_basePath);
    }

    public Task EnsureBucketExistsAsync()
    {
        Directory.CreateDirectory(_basePath);
        _logger.LogInformation("Local file storage initialized at '{Path}'", _basePath);
        return Task.CompletedTask;
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType)
    {
        var fullPath = Path.Combine(_basePath, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs);
        return key;
    }

    public async Task<Stream> DownloadAsync(string key)
    {
        var fullPath = Path.Combine(_basePath, key.Replace('/', Path.DirectorySeparatorChar));
        var ms = new MemoryStream();
        using var fs = File.OpenRead(fullPath);
        await fs.CopyToAsync(ms);
        ms.Position = 0;
        return ms;
    }

    public Task DeleteAsync(string key)
    {
        var fullPath = Path.Combine(_basePath, key.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
