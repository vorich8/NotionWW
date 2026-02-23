using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public interface IFastInvestService
    {
        // Инвестиции
        Task<FastInvestInvestment> CreateInvestmentAsync(int contactId, decimal depositAmount,
            DateTime plannedWithdrawalDate, string? comments = null);
        Task<FastInvestInvestment?> GetInvestmentAsync(int investmentId);
        Task<List<FastInvestInvestment>> GetInvestmentsByStatusAsync(InvestStatus status);
        Task<List<FastInvestInvestment>> GetInvestmentsByContactAsync(int contactId);
        Task<List<FastInvestInvestment>> GetAllInvestmentsAsync();
        Task<bool> CompleteInvestmentAsync(int investmentId, decimal withdrawalAmount, DateTime actualWithdrawalDate);
        Task<bool> WithdrawInvestmentAsync(int investmentId, decimal withdrawalAmount, DateTime actualWithdrawalDate);
        Task<bool> UpdateInvestmentAsync(FastInvestInvestment investment);
        Task<bool> DeleteInvestmentAsync(int investmentId);

        // Статистика
        Task<FastInvestStatistics> GetFastInvestStatisticsAsync();
    }

    public class FastInvestStatistics
    {
        public int TotalInvestors { get; set; }
        public int ActiveInvestors { get; set; }
        public int CompletedInvestors { get; set; }
        public decimal TotalActiveDeposits { get; set; }
        public decimal TotalCompletedDeposits { get; set; }
        public decimal TotalExpectedProfit { get; set; }
        public decimal TotalProfit { get; set; }
        public Dictionary<string, decimal> TopInvestors { get; set; } = new();
    }
}