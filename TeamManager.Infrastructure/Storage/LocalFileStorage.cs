using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TeamManager.Application.Abstractions.Storage;

namespace TeamManager.Infrastructure.Storage
{
    public sealed class LocalFileStorage(IOptions<FileStorageOptions> options, IHostEnvironment environment) : IFileStorage
    {
        private readonly string _rootPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.RootPath));

        public async Task<string> SaveAsync(Stream content, string storageKey, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(_rootPath);

            var fullPath = Path.Combine(_rootPath, storageKey);

            await using var fileStream = File.Create(fullPath);

            await content.CopyToAsync(fileStream, cancellationToken);

            return storageKey;
        }

        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
        {
            var fullPath = Path.Combine(_rootPath, storageKey);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Attachment file not found on disk.");

            return Task.FromResult<Stream>(File.OpenRead(fullPath));
        }

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
        {
            var fullPath = Path.Combine(_rootPath, storageKey);

            if (File.Exists(fullPath))
                File.Delete(fullPath);

            return Task.CompletedTask;
        }
    }
}
