using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public class DbFunPayService : IDbFunPayService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DbFunPayService> _logger;

        public DbFunPayService(ApplicationDbContext context, ILogger<DbFunPayService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ===== АККАУНТЫ =====

        public async Task<DbFunPayAccount> CreateAccountAsync(DbFunPayAccount account)
        {
            try
            {
                account.CreatedAt = DateTime.UtcNow;
                _context.DbFunPayAccounts.Add(account);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created FunPay account: {Nickname}", account.Nickname);
                return account;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating FunPay account");
                throw;
            }
        }

        public async Task<DbFunPayAccount?> GetAccountAsync(int accountId)
        {
            return await _context.DbFunPayAccounts
                .Include(a => a.Warnings)
                .FirstOrDefaultAsync(a => a.Id == accountId);
        }

        public async Task<List<DbFunPayAccount>> GetAllAccountsAsync()
        {
            return await _context.DbFunPayAccounts
                .Include(a => a.Warnings)
                .OrderBy(a => a.Nickname)
                .ToListAsync();
        }

        public async Task<bool> UpdateAccountAsync(DbFunPayAccount account)
        {
            try
            {
                var existing = await _context.DbFunPayAccounts.FindAsync(account.Id);
                if (existing == null) return false;

                existing.Nickname = account.Nickname;
                existing.GoldenKey = account.GoldenKey;
                existing.BotUsername = account.BotUsername;
                existing.BotPassword = account.BotPassword;
                existing.ApiKey = account.ApiKey;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated FunPay account: {Nickname}", account.Nickname);
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
                var account = await _context.DbFunPayAccounts
                    .Include(a => a.Warnings)
                    .FirstOrDefaultAsync(a => a.Id == accountId);

                if (account == null) return false;

                // Удаляем связанные штрафы
                _context.DbFunPayWarnings.RemoveRange(account.Warnings);
                _context.DbFunPayAccounts.Remove(account);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted FunPay account: {Nickname}", account.Nickname);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting FunPay account {AccountId}", accountId);
                return false;
            }
        }

        // ===== ШТРАФЫ =====

        public async Task<DbFunPayWarning> AddWarningAsync(int accountId, string reason)
        {
            try
            {
                var warning = new DbFunPayWarning
                {
                    FunPayAccountId = accountId,
                    Date = DateTime.UtcNow,
                    Reason = reason,
                    Status = "Активно"
                };

                _context.DbFunPayWarnings.Add(warning);
                await _context.SaveChangesAsync();

                _logger.LogWarning("Added warning to account {AccountId}: {Reason}", accountId, reason);
                return warning;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding warning to account {AccountId}", accountId);
                throw;
            }
        }

        public async Task<List<DbFunPayWarning>> GetWarningsAsync(int accountId)
        {
            return await _context.DbFunPayWarnings
                .Where(w => w.FunPayAccountId == accountId)
                .OrderByDescending(w => w.Date)
                .ToListAsync();
        }

        public async Task<List<DbFunPayWarning>> GetAllWarningsAsync()
        {
            return await _context.DbFunPayWarnings
                .Include(w => w.Account)
                .OrderByDescending(w => w.Date)
                .ToListAsync();
        }

        public async Task<bool> ResolveWarningAsync(int warningId, string resolution)
        {
            try
            {
                var warning = await _context.DbFunPayWarnings.FindAsync(warningId);
                if (warning == null) return false;

                warning.Resolution = resolution;
                warning.Status = "Решено";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Resolved warning {WarningId}", warningId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving warning {WarningId}", warningId);
                return false;
            }
        }

        public async Task<bool> DeleteWarningAsync(int warningId)
        {
            Console.WriteLine($"\n📦 DbFunPayService.DeleteWarningAsync для ID: {warningId}");

            try
            {
                var warning = await _context.DbFunPayWarnings.FindAsync(warningId);
                if (warning == null)
                {
                    Console.WriteLine($"❌ Штраф {warningId} не найден в БД");
                    return false;
                }

                Console.WriteLine($"✅ Найден штраф, удаляем...");
                _context.DbFunPayWarnings.Remove(warning);
                var result = await _context.SaveChangesAsync();

                Console.WriteLine($"✅ SaveChangesAsync вернул {result}");
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка: {ex.Message}");
                return false;
            }
        }

        // ===== ПОИСК =====

        public async Task<List<DbFunPayAccount>> SearchAccountsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<DbFunPayAccount>();

            searchTerm = searchTerm.ToLower();
            return await _context.DbFunPayAccounts
                .Where(a => a.Nickname.ToLower().Contains(searchTerm) ||
                            (a.BotUsername != null && a.BotUsername.ToLower().Contains(searchTerm)))
                .Include(a => a.Warnings)
                .Take(20)
                .ToListAsync();
        }

        // ===== СТАТИСТИКА =====

        public async Task<FunPayDbStatistics> GetStatisticsAsync()
        {
            var accounts = await _context.DbFunPayAccounts.ToListAsync();
            var warnings = await _context.DbFunPayWarnings.ToListAsync();

            return new FunPayDbStatistics
            {
                TotalAccounts = accounts.Count,
                TotalWarnings = warnings.Count,
                ActiveWarnings = warnings.Count(w => w.Status == "Активно"),
                ResolvedWarnings = warnings.Count(w => w.Status == "Решено")
            };
        }
    }
}