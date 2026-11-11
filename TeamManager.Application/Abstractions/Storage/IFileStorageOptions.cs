namespace TeamManager.Application.Abstractions.Storage
{
    public interface IFileStorageOptions
    {
        long MaxFileSizeBytes { get; }

        IReadOnlyCollection<string> AllowedExtensions { get; }
    }
}