using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public class FunPayService : IFunPayService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FunPayService> _logger;

        public FunPayService(ApplicationDbContext context, ILogger<FunPayService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ========== ПРОДАЖИ ==========

        public async Task<FunPaySale> CreateSaleAsync(string orderNumber, decimal saleAmount, decimal purchaseAmount,
            int quantity, string category, bool isBulkPurchase)
        {
            try
            {
                var sale = new FunPaySale
                {
                    OrderNumber = orderNumber,
                    SaleAmount = saleAmount,
                    PurchaseAmount = purchaseAmount,
                    Quantity = quantity,
                    Category = category,
                    IsBulkPurchase = isBulkPurchase,
                    SaleDate = DateTime.UtcNow
                };

                _context.FunPaySales.Add(sale);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created FunPay sale #{OrderNumber} with profit {Profit}",
                    orderNumber, sale.Profit);
                return sale;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating FunPay sale #{OrderNumber}", orderNumber);
                throw;
            }
        }

        public async Task<FunPaySale?> GetSaleAsync(int saleId)
        {
            return await _context.FunPaySales.FindAsync(saleId);
        }

        public async Task<List<FunPaySale>> GetAllSalesAsync()
        {
            return await _context.FunPaySales
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();
        }

        public async Task<List<FunPaySale>> GetSalesByDateRangeAsync(DateTime start, DateTime end)
        {
            return await _context.FunPaySales
                .Where(s => s.SaleDate >= start && s.SaleDate <= end)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();
        }

        public async Task<List<FunPaySale>> GetSalesByCategoryAsync(string category)
        {
            return await _context.FunPaySales
                .Where(s => s.Category == category)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();
        }

        public async Task<bool> UpdateSaleAsync(FunPaySale sale)
        {
            try
            {
                var existing = await _context.FunPaySales.FindAsync(sale.Id);
                if (existing == null) return false;

                existing.OrderNumber = sale.OrderNumber;
                existing.SaleAmount = sale.SaleAmount;
                existing.PurchaseAmount = sale.PurchaseAmount;
                existing.Quantity = sale.Quantity;
                existing.Category = sale.Category;
                existing.IsBulkPurchase = sale.IsBulkPurchase;
                existing.SaleDate = sale.SaleDate;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated FunPay sale #{OrderNumber}", sale.OrderNumber);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating FunPay sale {SaleId}", sale.Id);
                return false;
            }
        }

        public async Task<bool> DeleteSaleAsync(int saleId)
        {
            try
            {
                var sale = await _context.FunPaySales.FindAsync(saleId);
                if (sale == null) return false;

                _context.FunPaySales.Remove(sale);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted FunPay sale #{OrderNumber}", sale.OrderNumber);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting FunPay sale {SaleId}", saleId);
                return false;
            }
        }

        // ========== ВЫВОДЫ ==========

        public async Task<FunPayWithdrawal> CreateWithdrawalAsync(decimal amount, string destination, string description)
        {
            try
            {
                var withdrawal = new FunPayWithdrawal
                {
                    Amount = amount,
                    Destination = destination,
                    Description = description,
                    WithdrawalDate = DateTime.UtcNow
                };

                _context.FunPayWithdrawals.Add(withdrawal);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created FunPay withdrawal {Amount} to {Destination}", amount, destination);
                return withdrawal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating FunPay withdrawal");
                throw;
            }
        }

        public async Task<List<FunPayWithdrawal>> GetAllWithdrawalsAsync()
        {
            return await _context.FunPayWithdrawals
                .OrderByDescending(w => w.WithdrawalDate)
                .ToListAsync();
        }

        public async Task<bool> UpdateWithdrawalAsync(FunPayWithdrawal withdrawal)
        {
            try
            {
                var existing = await _context.FunPayWithdrawals.FindAsync(withdrawal.Id);
                if (existing == null) return false;

                existing.Amount = withdrawal.Amount;
                existing.Destination = withdrawal.Destination;
                existing.Description = withdrawal.Description;
                existing.WithdrawalDate = withdrawal.WithdrawalDate;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating withdrawal {WithdrawalId}", withdrawal.Id);
                return false;
            }
        }

        public async Task<bool> DeleteWithdrawalAsync(int withdrawalId)
        {
            try
            {
                var withdrawal = await _context.FunPayWithdrawals.FindAsync(withdrawalId);
                if (withdrawal == null) return false;

                _context.FunPayWithdrawals.Remove(withdrawal);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting withdrawal {WithdrawalId}", withdrawalId);
                return false;
            }
        }

        // ========== АККАУНТЫ ==========

        public async Task<FunPayAccount> CreateAccountAsync(string nickname, string goldenKey, string botUsername,
            string botPassword, string apiKey)
        {
            try
            {
                var account = new FunPayAccount
                {
                    Nickname = nickname,
                    GoldenKey = goldenKey,
                    BotUsername = botUsername,
                    BotPassword = botPassword,
                    ApiKey = apiKey
                };

                _context.FunPayAccounts.Add(account);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created FunPay account {Nickname}", nickname);
                return account;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating FunPay account {Nickname}", nickname);
                throw;
            }
        }

        public async Task<FunPayAccount?> GetAccountAsync(int accountId)
        {
            return await _context.FunPayAccounts
                .Include(a => a.Warnings)
                .FirstOrDefaultAsync(a => a.Id == accountId);
        }

        public async Task<List<FunPayAccount>> GetAllAccountsAsync()
        {
            return await _context.FunPayAccounts
                .Include(a => a.Warnings)
                .ToListAsync();
        }

        public async Task<bool> UpdateAccountAsync(FunPayAccount account)
        {
            try
            {
                var existing = await _context.FunPayAccounts.FindAsync(account.Id);
                if (existing == null) return false;

                existing.Nickname = account.Nickname;
                existing.GoldenKey = account.GoldenKey;
                existing.BotUsername = account.BotUsername;
                existing.BotPassword = account.BotPassword;
                existing.ApiKey = account.ApiKey;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating FunPay account {AccountId}", account.Id);
                return false;
            }
        }

        public async Task<bool> DeleteAccountAsync(int accountId)
        {
            try
            {
                var account = await _context.FunPayAccounts
                    .Include(a => a.Warnings)
                    .FirstOrDefaultAsync(a => a.Id == accountId);

                if (account == null) return false;

                _context.FunPayWarnings.RemoveRange(account.Warnings);
                _context.FunPayAccounts.Remove(account);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting FunPay account {AccountId}", accountId);
                return false;
            }
        }

        // ========== ШТРАФЫ ==========

        public async Task<FunPayWarning> AddWarningAsync(int accountId, string reason)
        {
            try
            {
                var warning = new FunPayWarning
                {
                    FunPayAccountId = accountId,
                    Date = DateTime.UtcNow,
                    Reason = reason
                };

                _context.FunPayWarnings.Add(warning);
                await _context.SaveChangesAsync();

                _logger.LogWarning("Added warning to FunPay account {AccountId}: {Reason}", accountId, reason);
                return warning;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding warning to account {AccountId}", accountId);
                throw;
            }
        }

        public async Task<List<FunPayWarning>> GetWarningsAsync(int accountId)
        {
            return await _context.FunPayWarnings
                .Where(w => w.FunPayAccountId == accountId)
                .OrderByDescending(w => w.Date)
                .ToListAsync();
        }

        public async Task<bool> ResolveWarningAsync(int warningId, string resolution)
        {
            try
            {
                var warning = await _context.FunPayWarnings.FindAsync(warningId);
                if (warning == null) return false;

                warning.Resolution = resolution;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Resolved warning {WarningId} with resolution: {Resolution}", warningId, resolution);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving warning {WarningId}", warningId);
                return false;
            }
        }

        // ========== СТАТИСТИКА ==========

        public async Task<FunPayStatistics> GetFunPayStatisticsAsync(DateTime? start = null, DateTime? end = null)
        {
            start ??= DateTime.UtcNow.AddMonths(-1);
            end ??= DateTime.UtcNow;

            var sales = await _context.FunPaySales
                .Where(s => s.SaleDate >= start && s.SaleDate <= end)
                .ToListAsync();

            var withdrawals = await _context.FunPayWithdrawals
                .Where(w => w.WithdrawalDate >= start && w.WithdrawalDate <= end)
                .ToListAsync();

            var warnings = await _context.FunPayWarnings
                .Where(w => w.Date >= start && w.Date <= end)
                .CountAsync();

            var stats = new FunPayStatistics
            {
                TotalSales = sales.Sum(s => s.TotalSaleAmount),
                TotalProfit = sales.Sum(s => s.Profit),
                TotalOrders = sales.Count,
                TotalWithdrawals = withdrawals.Sum(w => w.Amount),
                SalesByCategory = sales
                    .GroupBy(s => s.Category)
                    .ToDictionary(g => g.Key, g => g.Sum(s => s.TotalSaleAmount)),
                TotalWarnings = warnings
            };

            return stats;
        }
    }
}