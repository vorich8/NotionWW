using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public interface IManualService
    {
        // CRUD операции
        Task<DbManual> CreateManualAsync(DbManual manual);
        Task<DbManual?> GetManualAsync(int manualId);
        Task<List<DbManual>> GetAllManualsAsync();
        Task<List<DbManual>> GetManualsByCategoryAsync(string category);
        Task<List<DbManual>> GetManualsByBankAsync(string bankName);
        Task<bool> UpdateManualAsync(DbManual manual);
        Task<bool> DeleteManualAsync(int manualId);

        // Поиск
        Task<List<DbManual>> SearchManualsAsync(string searchTerm);

        // Статистика
        Task<ManualStatistics> GetManualStatisticsAsync();
    }

    public class ManualStatistics
    {
        public int TotalManuals { get; set; }
        public Dictionary<string, int> ManualsByCategory { get; set; } = new();
        public Dictionary<string, int> ManualsByBank { get; set; } = new();
    }
}