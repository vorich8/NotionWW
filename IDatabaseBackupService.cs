using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public interface IDatabaseBackupService
    {
        Task<BackupResult> CreateBackupAsync();
        string GetBackupFolderPath();
        Task CleanupOldBackupsAsync(int keepLast = 10);
    }

    public class BackupResult
    {
        public bool Success { get; set; }
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
        public long FileSize { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}