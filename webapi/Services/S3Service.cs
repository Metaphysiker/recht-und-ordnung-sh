using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;

namespace webapi.Services;

public interface IS3Service
{
    Task EnsureBucketExistsAsync();
    Task<string> UploadAsync(string key, Stream content, string contentType);
    Task<Stream> DownloadAsync(string key);
    Task DeleteAsync(string key);
}

public class S3Service : IS3Service
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucketName;
    private readonly ILogger<S3Service> _logger;

    public S3Service(IAmazonS3 s3, IConfiguration configuration, ILogger<S3Service> logger)
    {
        _s3 = s3;
        _bucketName = configuration["AWS_BUCKET_NAME"] ?? throw new InvalidOperationException("AWS_BUCKET_NAME not configured");
        _logger = logger;
    }

    public async Task EnsureBucketExistsAsync()
    {
        var exists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3, _bucketName);
        if (!exists)
        {
            await _s3.PutBucketAsync(new PutBucketRequest { BucketName = _bucketName, UseClientRegion = true });
            _logger.LogInformation("Created S3 bucket '{BucketName}'", _bucketName);
        }
        else
        {
            _logger.LogInformation("S3 bucket '{BucketName}' already exists", _bucketName);
        }
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType)
    {
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
        });
        return key;
    }

    public async Task<Stream> DownloadAsync(string key)
    {
        using var response = await _s3.GetObjectAsync(_bucketName, key);
        var ms = new MemoryStream();
        await response.ResponseStream.CopyToAsync(ms);
        ms.Position = 0;
        return ms;
    }

    public async Task DeleteAsync(string key)
    {
        await _s3.DeleteObjectAsync(_bucketName, key);
    }
}
