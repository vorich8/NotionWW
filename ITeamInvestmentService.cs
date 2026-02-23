using TeamManagerBot.Models;

public interface ITeamInvestmentService
{
    Task<TeamInvestment> CreateInvestmentAsync(long userId, decimal amount, string? description = null);
    Task<TeamInvestment?> GetInvestmentAsync(int investmentId);
    Task<List<TeamInvestment>> GetUserInvestmentsAsync(long userId);
    Task<List<TeamInvestment>> GetAllActiveInvestmentsAsync();
    Task<bool> WithdrawFromInvestmentAsync(int investmentId, decimal amount, string? description = null);
    Task<decimal> GetUserTotalInvestedAsync(long userId);
    Task<decimal> GetUserCurrentBalanceAsync(long userId);
}