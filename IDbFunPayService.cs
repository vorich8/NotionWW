using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public interface IDbFunPayService
    {
        // CRUD для аккаунтов
        Task<DbFunPayAccount> CreateAccountAsync(DbFunPayAccount account);
        Task<DbFunPayAccount?> GetAccountAsync(int accountId);
        Task<List<DbFunPayAccount>> GetAllAccountsAsync();
        Task<bool> UpdateAccountAsync(DbFunPayAccount account);
        Task<bool> DeleteAccountAsync(int accountId);

        // CRUD для штрафов
        Task<DbFunPayWarning> AddWarningAsync(int accountId, string reason);
        Task<List<DbFunPayWarning>> GetWarningsAsync(int accountId);
        Task<List<DbFunPayWarning>> GetAllWarningsAsync();
        Task<bool> ResolveWarningAsync(int warningId, string resolution);
        Task<bool> DeleteWarningAsync(int warningId);

        // Поиск
        Task<List<DbFunPayAccount>> SearchAccountsAsync(string searchTerm);

        // Статистика
        Task<FunPayDbStatistics> GetStatisticsAsync();
    }

    public class FunPayDbStatistics
    {
        public int TotalAccounts { get; set; }
        public int TotalWarnings { get; set; }
        public int ActiveWarnings { get; set; }
        public int ResolvedWarnings { get; set; }
    }
}