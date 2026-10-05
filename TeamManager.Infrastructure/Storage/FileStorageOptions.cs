namespace TeamManager.Infrastructure.Storage
{
    public sealed class FileStorageOptions
    {
        public const string SectionName = "FileStorage";

        public string RootPath { get; set; } = string.Empty;

        public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

        public string[] AllowedExtensions { get; set; } = [".pdf", ".png", ".jpg", ".jpeg", ".docx", ".xlsx"];
    }
}