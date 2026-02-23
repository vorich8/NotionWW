using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public interface IAdService
    {
        // CRUD операции
        Task<DbAd> CreateAdAsync(DbAd ad);
        Task<DbAd?> GetAdAsync(int adId);
        Task<List<DbAd>> GetAllAdsAsync();
        Task<List<DbAd>> GetAdsByProjectAsync(string projectName);
        Task<List<DbAd>> GetAdsByStatusAsync(string status);
        Task<List<DbAd>> GetAdsByDateRangeAsync(DateTime start, DateTime end);
        Task<bool> UpdateAdAsync(DbAd ad);
        Task<bool> DeleteAdAsync(int adId);

        // Финансы
        Task<bool> AddSpentAsync(int adId, decimal amount);

        // Поиск
        Task<List<DbAd>> SearchAdsAsync(string searchTerm);

        // Статистика
        Task<AdStatistics> GetAdStatisticsAsync();
    }
}