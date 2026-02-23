using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public interface ICommissionService
    {
        // Комиссии
        Task<BankCommission> CreateCommissionAsync(BankCommission commission);
        Task<BankCommission?> GetCommissionAsync(int commissionId);
        Task<List<BankCommission>> GetAllCommissionsAsync();
        Task<List<BankCommission>> GetCommissionsByBankAsync(string bankName);
        Task<List<BankCommission>> GetCommissionsByCategoryAsync(string category);
        Task<Dictionary<string, List<BankCommission>>> GetCommissionsGroupedByBankAsync();
        Task<bool> UpdateCommissionAsync(BankCommission commission);
        Task<bool> DeleteCommissionAsync(int commissionId);

        // Советы
        Task<CommissionTip> CreateTipAsync(CommissionTip tip);
        Task<CommissionTip?> GetTipAsync(int tipId);
        Task<List<CommissionTip>> GetAllTipsAsync();
        Task<List<CommissionTip>> GetTipsByCategoryAsync(string category);
        Task<List<CommissionTip>> GetTipsByBankAsync(string bankName);
        Task<bool> UpdateTipAsync(CommissionTip tip);
        Task<bool> DeleteTipAsync(int tipId);

        // Расчеты
        Task<decimal> CalculateCommissionAsync(string bankName, string category, decimal amount);
        Task<string> GetAdviceForTransactionAsync(string bankName, string category, decimal amount);
    }
}