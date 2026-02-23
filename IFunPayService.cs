using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public interface IFunPayService
    {
        // Продажи
        Task<FunPaySale> CreateSaleAsync(string orderNumber, decimal saleAmount, decimal purchaseAmount,
            int quantity, string category, bool isBulkPurchase);
        Task<FunPaySale?> GetSaleAsync(int saleId);
        Task<List<FunPaySale>> GetAllSalesAsync();
        Task<List<FunPaySale>> GetSalesByDateRangeAsync(DateTime start, DateTime end);
        Task<List<FunPaySale>> GetSalesByCategoryAsync(string category);
        Task<bool> UpdateSaleAsync(FunPaySale sale);
        Task<bool> DeleteSaleAsync(int saleId);

        // Выводы
        Task<FunPayWithdrawal> CreateWithdrawalAsync(decimal amount, string destination, string description);
        Task<List<FunPayWithdrawal>> GetAllWithdrawalsAsync();
        Task<bool> UpdateWithdrawalAsync(FunPayWithdrawal withdrawal);
        Task<bool> DeleteWithdrawalAsync(int withdrawalId);

        // Аккаунты
        Task<FunPayAccount> CreateAccountAsync(string nickname, string goldenKey, string botUsername,
            string botPassword, string apiKey);
        Task<FunPayAccount?> GetAccountAsync(int accountId);
        Task<List<FunPayAccount>> GetAllAccountsAsync();
        Task<bool> UpdateAccountAsync(FunPayAccount account);
        Task<bool> DeleteAccountAsync(int accountId);

        // Штрафы
        Task<FunPayWarning> AddWarningAsync(int accountId, string reason);
        Task<List<FunPayWarning>> GetWarningsAsync(int accountId);
        Task<bool> ResolveWarningAsync(int warningId, string resolution);

        // Статистика
        Task<FunPayStatistics> GetFunPayStatisticsAsync(DateTime? start = null, DateTime? end = null);
    }

    public class FunPayStatistics
    {
        public decimal TotalSales { get; set; }
        public decimal TotalProfit { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalWithdrawals { get; set; }
        public Dictionary<string, decimal> SalesByCategory { get; set; } = new();
        public int TotalWarnings { get; set; }
    }
}