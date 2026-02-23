using Microsoft.Extensions.Logging;
using TeamManagerBot.Models;
using Microsoft.EntityFrameworkCore; 
using System.Linq; 

public class TeamInvestmentService : ITeamInvestmentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TeamInvestmentService> _logger;

    public TeamInvestmentService(ApplicationDbContext context, ILogger<TeamInvestmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TeamInvestment> CreateInvestmentAsync(long userId, decimal amount, string? description = null)
    {
        var investment = new TeamInvestment
        {
            UserId = userId,
            InitialAmount = amount,
            CurrentAmount = amount,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.TeamInvestments.Add(investment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created investment for user {UserId}: {Amount}", userId, amount);
        return investment;
    }

    public async Task<TeamInvestment?> GetInvestmentAsync(int investmentId)
    {
        return await _context.TeamInvestments
            .Include(i => i.Withdrawals)
            .FirstOrDefaultAsync(i => i.Id == investmentId);
    }

    public async Task<List<TeamInvestment>> GetUserInvestmentsAsync(long userId)
    {
        return await _context.TeamInvestments
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<TeamInvestment>> GetAllActiveInvestmentsAsync()
    {
        return await _context.TeamInvestments
            .Where(i => i.IsActive && i.CurrentAmount > 0)
            .Include(i => i.User)
            .ToListAsync();
    }

    public async Task<bool> WithdrawFromInvestmentAsync(int investmentId, decimal amount, string? description = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var investment = await _context.TeamInvestments.FindAsync(investmentId);
            if (investment == null)
                return false;

            if (investment.CurrentAmount < amount)
                return false;

            // Создаем запись о выводе
            var withdrawal = new InvestmentWithdrawal
            {
                InvestmentId = investmentId,
                Amount = amount,
                Description = description,
                WithdrawnAt = DateTime.UtcNow
            };

            // Уменьшаем текущую сумму вклада
            investment.CurrentAmount -= amount;
            investment.LastWithdrawalAt = DateTime.UtcNow;

            if (investment.CurrentAmount == 0)
            {
                investment.IsActive = false;
            }

            _context.InvestmentWithdrawals.Add(withdrawal);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Withdrawn {Amount} from investment {InvestmentId}", amount, investmentId);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error withdrawing from investment {InvestmentId}", investmentId);
            return false;
        }
    }

    public async Task<decimal> GetUserTotalInvestedAsync(long userId)
    {
        return await _context.TeamInvestments
            .Where(i => i.UserId == userId)
            .SumAsync(i => i.InitialAmount);
    }

    public async Task<decimal> GetUserCurrentBalanceAsync(long userId)
    {
        return await _context.TeamInvestments
            .Where(i => i.UserId == userId && i.IsActive)
            .SumAsync(i => i.CurrentAmount);
    }
}