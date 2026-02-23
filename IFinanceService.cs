using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public interface IFinanceService
    {
        // Добавляем enum для статусов (можно вынести в отдельный файл)
        public enum FundStatus
        {
            Working = 1,      // В обороте (рабочие)
            Reserved = 2,      // Резерв (нерабочие)
            Blocked = 3,       // Заблокировано
            InTransit = 4      // В пути (для крипты/переводов)
        }

        // ОБНОВЛЕН: расширенный метод создания с новыми параметрами
        Task<FinancialRecord?> CreateFinancialRecordAsync(
            FinancialRecordType type,
            string category,
            string description,
            decimal amount,
            string currency,
            string? source,
            long? userId,
            int? projectId,
            FundStatus fundStatus = FundStatus.Working,
            int? contactId = null,
            decimal? commission = null,
            string? commissionPaidBy = null);

        // НОВЫЙ: создать комиссию
        Task<FinancialRecord?> CreateCommissionRecordAsync(
            decimal amount,
            string currency,
            string description,
            int? projectId,
            long? userId,
            string commissionType = "transfer");

        // НОВЫЙ: получить баланс с разделением по статусам
        Task<Dictionary<FundStatus, decimal>> GetBalanceByStatusAsync(string? currency = null);

        // НОВЫЙ: переместить средства между статусами
        Task<bool> TransferFundsBetweenStatusesAsync(
            int recordId,
            FundStatus newStatus,
            string reason,
            long? userId = null);

        // НОВЫЙ: получить статистику по комиссиям
        Task<CommissionStatistics> GetCommissionStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

        // НОВЫЙ: получить все записи по контакту
        Task<List<FinancialRecord>> GetRecordsByContactAsync(int contactId);

        // Существующие методы (некоторые обновлены)
        Task<List<FinancialRecord>> GetAllRecordsAsync();
        Task<List<FinancialRecord>> GetRecordsByTypeAsync(FinancialRecordType type);
        Task<List<FinancialRecord>> GetRecordsByCategoryAsync(string category);
        Task<List<FinancialRecord>> GetRecordsByDateRangeAsync(DateTime startDate, DateTime endDate);

        Task<FinancialRecord?> GetRecordAsync(int recordId);
        Task<bool> UpdateRecordAsync(FinancialRecord record);
        Task<bool> DeleteRecordAsync(int recordId);

        // ОБНОВЛЕН: статистика теперь включает workingCapital и reservedFunds
        Task<FinanceStatistics> GetFinanceStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

        Task<decimal> GetTotalBalanceAsync();
        Task<decimal> GetTotalIncomeAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<decimal> GetTotalExpensesAsync(DateTime? startDate = null, DateTime? endDate = null);

        Task<List<CategoryStat>> GetIncomeByCategoryAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<CategoryStat>> GetExpensesByCategoryAsync(DateTime? startDate = null, DateTime? endDate = null);

        Task<Dictionary<string, decimal>> GetMonthlyTrendAsync(FinancialRecordType type, int months = 6);

        Task<bool> RecordExistsAsync(int recordId);
        Task<int> GetRecordsCountAsync();

        Task<byte[]> GenerateMonthlyExpensesChartAsync(int year, int month);
    }

    // НОВЫЙ: модель для статистики комиссий
    public class CommissionStatistics
    {
        public decimal TotalCommissions { get; set; }
        public int CommissionCount { get; set; }
        public decimal AverageCommission { get; set; }
        public Dictionary<string, decimal> CommissionsByProject { get; set; } = new();
        public decimal LargestCommission { get; set; }
        public Dictionary<string, decimal> CommissionsByType { get; set; } = new(); // transfer, withdrawal, exchange
    }
}