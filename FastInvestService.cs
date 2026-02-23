using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public class FastInvestService : IFastInvestService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FastInvestService> _logger;
        private readonly IContactService _contactService;

        public FastInvestService(
            ApplicationDbContext context,
            ILogger<FastInvestService> logger,
            IContactService contactService)
        {
            _context = context;
            _logger = logger;
            _contactService = contactService;
        }

        // ========== ЛОГИКА РАСЧЁТА ПРИБЫЛИ ==========

        private (decimal percent, decimal profit) CalculateProfit(decimal amount, int days)
        {
            // Базовый процент 4% в неделю
            decimal weeklyPercent = 4m;

            // Количество полных недель (округляем вверх)
            int weeks = (days + 6) / 7; // Целочисленное деление с округлением вверх

            decimal basePercent = 0;

            // Расчёт базового процента в зависимости от количества недель
            switch (weeks)
            {
                case 1:
                    basePercent = 4m;
                    break;
                case 2:
                    basePercent = 9m; // 4% + 5% бонус
                    break;
                case 3:
                    basePercent = 13m; // 12% + 1% бонус
                    break;
                case 4:
                    basePercent = 18m; // 16% + 2% бонус
                    break;
                case 5:
                    basePercent = 23m; // 20% + 3% бонус
                    break;
                case 6:
                    basePercent = 28m; // 24% + 4% бонус
                    break;
                case 7:
                    basePercent = 34m; // 28% + 6% бонус
                    break;
                case 8:
                    basePercent = 40m; // 32% + 8% бонус
                    break;
                default:
                    // Для более 8 недель: (недели * 4%) + бонус за удержание
                    decimal baseWeeksProfit = weeklyPercent * weeks;
                    decimal bonus = weeks switch
                    {
                        > 12 => 15m,
                        > 10 => 12m,
                        > 8 => 10m,
                        _ => 8m
                    };
                    basePercent = baseWeeksProfit + bonus;
                    break;
            }

            // Бонус за большую сумму (больше 50к)
            if (amount >= 50000)
            {
                basePercent += 1m; // +1% ко всей сумме за весь срок
            }

            // Расчёт прибыли
            decimal profit = amount * basePercent / 100;

            return (basePercent, profit);
        }

        // ========== ИНВЕСТИЦИИ ==========

        public async Task<FastInvestInvestment> CreateInvestmentAsync(int contactId, decimal depositAmount,
            DateTime plannedWithdrawalDate, string? comments = null)
        {
            // Проверяем существование контакта
            var contact = await _context.Contacts.FindAsync(contactId);
            if (contact == null)
            {
                _logger.LogError("Contact with ID {ContactId} not found when creating investment", contactId);
                throw new InvalidOperationException($"Contact with ID {contactId} does not exist");
            }

            // Рассчитываем количество дней
            var days = (plannedWithdrawalDate - DateTime.UtcNow).Days;
            if (days <= 0)
            {
                throw new ArgumentException("Planned withdrawal date must be in the future");
            }

            // Рассчитываем прибыль
            var (percent, expectedProfit) = CalculateProfit(depositAmount, days);

            var investment = new FastInvestInvestment
            {
                ContactId = contactId,
                Investor = contact,
                DepositAmount = depositAmount,
                ExpectedProfitPercent = percent,
                ExpectedProfitAmount = expectedProfit,
                DepositDate = DateTime.UtcNow,
                PlannedWithdrawalDate = plannedWithdrawalDate,
                Comments = comments,
                Status = InvestStatus.Active,
            };

            _context.FastInvestInvestments.Add(investment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created FastInvest investment for contact {ContactId} with amount {Amount}, profit {Percent}% ({Profit} ₽)",
                contactId, depositAmount, percent, expectedProfit);
            return investment;
        }

        public async Task<FastInvestInvestment?> GetInvestmentAsync(int investmentId)
        {
            return await _context.FastInvestInvestments
                .Include(i => i.Investor)
                .FirstOrDefaultAsync(i => i.Id == investmentId);
        }

        public async Task<List<FastInvestInvestment>> GetInvestmentsByStatusAsync(InvestStatus status)
        {
            return await _context.FastInvestInvestments
                .Where(i => i.Status == status)
                .Include(i => i.Investor)
                .OrderByDescending(i => i.DepositDate)
                .ToListAsync();
        }

        public async Task<List<FastInvestInvestment>> GetInvestmentsByContactAsync(int contactId)
        {
            return await _context.FastInvestInvestments
                .Where(i => i.ContactId == contactId)
                .Include(i => i.Investor)
                .OrderByDescending(i => i.DepositDate)
                .ToListAsync();
        }

        public async Task<List<FastInvestInvestment>> GetAllInvestmentsAsync()
        {
            return await _context.FastInvestInvestments
                .Include(i => i.Investor)
                .OrderByDescending(i => i.DepositDate)
                .ToListAsync();
        }

        public async Task<bool> CompleteInvestmentAsync(int investmentId, decimal withdrawalAmount, DateTime actualWithdrawalDate)
        {
            try
            {
                var investment = await _context.FastInvestInvestments.FindAsync(investmentId);
                if (investment == null)
                {
                    _logger.LogWarning("Investment {InvestmentId} not found", investmentId);
                    return false;
                }

                investment.WithdrawalAmount = withdrawalAmount;
                investment.ActualWithdrawalDate = actualWithdrawalDate;
                investment.Status = InvestStatus.Withdrawn; // Меняем на Withdrawn, а не Completed

                await _context.SaveChangesAsync();

                _logger.LogInformation("Completed FastInvest investment {InvestmentId} with profit {Profit}",
                    investmentId, investment.Profit);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing investment {InvestmentId}", investmentId);
                return false;
            }
        }

        public async Task<bool> WithdrawInvestmentAsync(int investmentId, decimal withdrawalAmount, DateTime actualWithdrawalDate)
        {
            try
            {
                var investment = await _context.FastInvestInvestments.FindAsync(investmentId);
                if (investment == null)
                {
                    _logger.LogWarning("Investment {InvestmentId} not found", investmentId);
                    return false;
                }

                investment.WithdrawalAmount = withdrawalAmount;
                investment.ActualWithdrawalDate = actualWithdrawalDate;
                investment.Status = InvestStatus.Withdrawn;

                await _context.SaveChangesAsync();

                var actualProfit = withdrawalAmount - investment.DepositAmount;
                _logger.LogInformation("Withdrawn FastInvest investment {InvestmentId} with amount {Amount}, profit {Profit}",
                    investmentId, withdrawalAmount, actualProfit);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error withdrawing investment {InvestmentId}", investmentId);
                return false;
            }
        }

        public async Task<bool> UpdateInvestmentAsync(FastInvestInvestment investment)
        {
            try
            {
                var existing = await _context.FastInvestInvestments.FindAsync(investment.Id);
                if (existing == null) return false;

                existing.DepositAmount = investment.DepositAmount;
                existing.PlannedWithdrawalDate = investment.PlannedWithdrawalDate;
                existing.ActualWithdrawalDate = investment.ActualWithdrawalDate;
                existing.WithdrawalAmount = investment.WithdrawalAmount;
                existing.Status = investment.Status;
                existing.Comments = investment.Comments;
                existing.ExpectedProfitPercent = investment.ExpectedProfitPercent;
                existing.ExpectedProfitAmount = investment.ExpectedProfitAmount;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated FastInvest investment {InvestmentId}", investment.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating investment {InvestmentId}", investment.Id);
                return false;
            }
        }

        public async Task<bool> DeleteInvestmentAsync(int investmentId)
        {
            try
            {
                var investment = await _context.FastInvestInvestments.FindAsync(investmentId);
                if (investment == null) return false;

                _context.FastInvestInvestments.Remove(investment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted FastInvest investment {InvestmentId}", investmentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting investment {InvestmentId}", investmentId);
                return false;
            }
        }

        // ========== СТАТИСТИКА ==========

        public async Task<FastInvestStatistics> GetFastInvestStatisticsAsync()
        {
            var investments = await _context.FastInvestInvestments
                .Include(i => i.Investor)
                .ToListAsync();

            var activeInvestments = investments.Where(i => i.Status == InvestStatus.Active).ToList();
            var completedInvestments = investments.Where(i => i.Status == InvestStatus.Completed).ToList();
            var withdrawnInvestments = investments.Where(i => i.Status == InvestStatus.Withdrawn).ToList();

            var totalExpectedProfit = activeInvestments.Sum(i => i.ExpectedProfitAmount ?? 0);
            var totalActualProfit = completedInvestments.Sum(i => i.Profit ?? 0);

            var stats = new FastInvestStatistics
            {
                TotalInvestors = investments.Select(i => i.ContactId).Distinct().Count(),
                ActiveInvestors = activeInvestments.Select(i => i.ContactId).Distinct().Count(),
                CompletedInvestors = completedInvestments.Select(i => i.ContactId).Distinct().Count(),
                TotalActiveDeposits = activeInvestments.Sum(i => i.DepositAmount),
                TotalCompletedDeposits = completedInvestments.Sum(i => i.DepositAmount),
                TotalExpectedProfit = totalExpectedProfit,
                TotalProfit = totalActualProfit,
                TopInvestors = investments
                    .Where(i => i.Profit.HasValue)
                    .GroupBy(i => i.Investor?.FullName ?? "Unknown")
                    .OrderByDescending(g => g.Sum(i => i.Profit ?? 0))
                    .Take(5)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.Profit ?? 0))
            };

            return stats;
        }
    }
}