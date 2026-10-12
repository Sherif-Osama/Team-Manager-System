namespace TeamManager.Infrastructure.Storage
{
    public sealed class FileStorageOptions
    {
        public string Provider { get; set; } = "Local";
        public string BucketName { get; set; } = string.Empty;
        public string Region { get; set; } = "eu-north-1";
        public string ProfileName { get; set; } = string.Empty;
        public const string SectionName = "FileStorage";
        public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

        public string[] AllowedExtensions { get; set; } = [".pdf", ".png", ".jpg", ".jpeg", ".docx", ".xlsx"];
    }
}