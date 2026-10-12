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
            response = await s3.GetObjectAsync(
                new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = storageKey
                },
                cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
        {
            throw new FileNotFoundException("Attachment file not found in Amazon S3.", ex);
        }

        return new S3ObjectReadStream(response);
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        await s3.DeleteObjectAsync(
            new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = storageKey
            },
            cancellationToken);
    }

    private sealed class S3ObjectReadStream : Stream
    {
        private readonly GetObjectResponse _response;
        private readonly Stream _innerStream;
        private bool _disposed;

        public S3ObjectReadStream(GetObjectResponse response)
        {
            _response = response;
            _innerStream = response.ResponseStream;
        }

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanSeek => _innerStream.CanSeek;

        public override bool CanWrite => false;

        public override long Length => _innerStream.Length;

        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }

        public override void Flush() => _innerStream.Flush();

        public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);

        public override int Read(Span<byte> buffer) => _innerStream.Read(buffer);

        public override Task<int> ReadAsync(
byte[] buffer, int offset, int count,
            CancellationToken cancellationToken) => _innerStream.ReadAsync(buffer, offset, count, cancellationToken);


        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => _innerStream.ReadAsync(buffer, cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _response.Dispose();
            }

            base.Dispose(disposing);
        }

        public override ValueTask DisposeAsync()
        {
            Dispose(true);
            GC.SuppressFinalize(this);

            return ValueTask.CompletedTask;
        }
    }
}