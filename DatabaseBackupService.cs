using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public class DatabaseBackupService : IDatabaseBackupService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseBackupService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _backupFolder;

        public DatabaseBackupService(
            ApplicationDbContext context,
            ILogger<DatabaseBackupService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;

            // Определяем папку для бэкапов
            _backupFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");

            // Создаем папку если её нет
            if (!Directory.Exists(_backupFolder))
            {
                Directory.CreateDirectory(_backupFolder);
            }
        }

        public string GetBackupFolderPath() => _backupFolder;

        public async Task<BackupResult> CreateBackupAsync()
        {
            var result = new BackupResult();

            try
            {
                // Получаем строку подключения
                var connectionString = _configuration.GetConnectionString("DefaultConnection")
                    ?? _configuration["BotConfiguration:ConnectionString"];

                if (string.IsNullOrEmpty(connectionString))
                {
                    result.ErrorMessage = "Connection string not found";
                    return result;
                }

                // Извлекаем путь к файлу базы данных из строки подключения
                var dbPath = ExtractDatabasePath(connectionString);

                if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
                {
                    result.ErrorMessage = $"Database file not found at: {dbPath}";
                    return result;
                }

                // Создаем имя файла бэкапа с временной меткой
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"backup_{timestamp}.db";
                var backupFilePath = Path.Combine(_backupFolder, backupFileName);

                // Копируем файл базы данных
                File.Copy(dbPath, backupFilePath, true);

                // Создаем архив
                var zipFileName = $"backup_{timestamp}.zip";
                var zipFilePath = Path.Combine(_backupFolder, zipFileName);

                using (var zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                {
                    zip.CreateEntryFromFile(backupFilePath, backupFileName);
                }

                // Удаляем временный .db файл
                File.Delete(backupFilePath);

                // Получаем размер файла
                var fileInfo = new FileInfo(zipFilePath);

                result.Success = true;
                result.FilePath = zipFilePath;
                result.FileName = zipFileName;
                result.FileSize = fileInfo.Length;

                _logger.LogInformation("Backup created successfully: {FileName} ({FileSize} bytes)", zipFileName, fileInfo.Length);

                // Очищаем старые бэкапы
                await CleanupOldBackupsAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating backup");
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        public async Task CleanupOldBackupsAsync(int keepLast = 10)
        {
            try
            {
                var backupFiles = Directory.GetFiles(_backupFolder, "backup_*.zip")
                    .OrderByDescending(f => f)
                    .ToList();

                if (backupFiles.Count > keepLast)
                {
                    var filesToDelete = backupFiles.Skip(keepLast);
                    foreach (var file in filesToDelete)
                    {
                        File.Delete(file);
                        _logger.LogInformation("Deleted old backup: {File}", Path.GetFileName(file));
                    }
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old backups");
            }
        }

        private string? ExtractDatabasePath(string connectionString)
        {
            // Парсим строку подключения SQLite
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                {
                    var path = trimmed.Substring("Data Source=".Length);

                    // Если путь относительный, преобразуем в абсолютный
                    if (!Path.IsPathRooted(path))
                    {
                        path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
                    }

                    return path;
                }
            }

            return null;
        }
    }
}