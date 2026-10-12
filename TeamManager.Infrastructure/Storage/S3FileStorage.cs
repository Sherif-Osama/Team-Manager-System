using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using System.Net;
using TeamManager.Application.Abstractions.Storage;

namespace TeamManager.Infrastructure.Storage;

public sealed class S3FileStorage(IAmazonS3 s3, IOptions<FileStorageOptions> options) : IFileStorage
{
    private readonly string _bucketName = options.Value.BucketName;

    public async Task<string> SaveAsync(Stream content, string storageKey, CancellationToken cancellationToken)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = storageKey,
            InputStream = content,
            AutoCloseStream = false,
            AutoResetStreamPosition = false
        };

        await s3.PutObjectAsync(request, cancellationToken);

        return storageKey;
    }

    public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        GetObjectResponse response;

        try
        {
            response = await s3.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = storageKey
            }, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
        {
            throw new FileNotFoundException("Attachment file not found in Amazon S3.", ex);
        }

        return response.ResponseStream;
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        await s3.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = storageKey
        }, cancellationToken);
    }
}