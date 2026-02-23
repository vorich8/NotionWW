using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamManagerBot.Models;
using System.Text;

namespace TeamManagerBot.Services
{
    public class CommissionService : ICommissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CommissionService> _logger;

        public CommissionService(ApplicationDbContext context, ILogger<CommissionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ========== КОМИССИИ ==========

        public async Task<BankCommission> CreateCommissionAsync(BankCommission commission)
        {
            try
            {
                commission.CreatedAt = DateTime.UtcNow;

                _context.BankCommissions.Add(commission);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created commission for {BankName} - {Category}",
                    commission.BankName, commission.Category);
                return commission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating commission");
                throw;
            }
        }

        public async Task<BankCommission?> GetCommissionAsync(int commissionId)
        {
            return await _context.BankCommissions.FindAsync(commissionId);
        }

        public async Task<List<BankCommission>> GetAllCommissionsAsync()
        {
            return await _context.BankCommissions
                .OrderBy(c => c.BankName)
                .ThenBy(c => c.Priority)
                .ToListAsync();
        }

        public async Task<List<BankCommission>> GetCommissionsByBankAsync(string bankName)
        {
            return await _context.BankCommissions
                .Where(c => c.BankName.ToLower() == bankName.ToLower())
                .OrderBy(c => c.Category)
                .ToListAsync();
        }

        public async Task<List<BankCommission>> GetCommissionsByCategoryAsync(string category)
        {
            return await _context.BankCommissions
                .Where(c => c.Category.ToLower() == category.ToLower())
                .OrderBy(c => c.BankName)
                .ToListAsync();
        }

        public async Task<Dictionary<string, List<BankCommission>>> GetCommissionsGroupedByBankAsync()
        {
            var commissions = await _context.BankCommissions
                .OrderBy(c => c.BankName)
                .ThenBy(c => c.Priority)
                .ToListAsync();

            return commissions
                .GroupBy(c => c.BankName)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public async Task<bool> UpdateCommissionAsync(BankCommission commission)
        {
            try
            {
                var existing = await _context.BankCommissions.FindAsync(commission.Id);
                if (existing == null) return false;

                existing.BankName = commission.BankName;
                existing.Category = commission.Category;
                existing.CommissionType = commission.CommissionType;
                existing.PercentValue = commission.PercentValue;
                existing.FixedValue = commission.FixedValue;
                existing.FixedCurrency = commission.FixedCurrency;
                existing.MinAmount = commission.MinAmount;
                existing.MaxAmount = commission.MaxAmount;
                existing.Description = commission.Description;
                existing.Advice = commission.Advice;
                existing.Priority = commission.Priority;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated commission {CommissionId}", commission.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating commission {CommissionId}", commission.Id);
                return false;
            }
        }

        public async Task<bool> DeleteCommissionAsync(int commissionId)
        {
            try
            {
                var commission = await _context.BankCommissions.FindAsync(commissionId);
                if (commission == null) return false;

                _context.BankCommissions.Remove(commission);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted commission {CommissionId}", commissionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting commission {CommissionId}", commissionId);
                return false;
            }
        }

        // ========== СОВЕТЫ ==========

        public async Task<CommissionTip> CreateTipAsync(CommissionTip tip)
        {
            try
            {
                tip.CreatedAt = DateTime.UtcNow;

                _context.CommissionTips.Add(tip);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created tip: {Title}", tip.Title);
                return tip;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tip");
                throw;
            }
        }

        public async Task<CommissionTip?> GetTipAsync(int tipId)
        {
            return await _context.CommissionTips.FindAsync(tipId);
        }

        public async Task<List<CommissionTip>> GetAllTipsAsync()
        {
            return await _context.CommissionTips
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.Category)
                .ToListAsync();
        }

        public async Task<List<CommissionTip>> GetTipsByCategoryAsync(string category)
        {
            return await _context.CommissionTips
                .Where(t => t.Category == category)
                .OrderByDescending(t => t.Priority)
                .ToListAsync();
        }

        public async Task<List<CommissionTip>> GetTipsByBankAsync(string bankName)
        {
            return await _context.CommissionTips
                .Where(t => t.BankName == bankName || t.BankName == null)
                .OrderByDescending(t => t.Priority)
                .ToListAsync();
        }

        public async Task<bool> UpdateTipAsync(CommissionTip tip)
        {
            try
            {
                var existing = await _context.CommissionTips.FindAsync(tip.Id);
                if (existing == null) return false;

                existing.Title = tip.Title;
                existing.Content = tip.Content;
                existing.Category = tip.Category;
                existing.BankName = tip.BankName;
                existing.Priority = tip.Priority;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated tip {TipId}", tip.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tip {TipId}", tip.Id);
                return false;
            }
        }

        public async Task<bool> DeleteTipAsync(int tipId)
        {
            try
            {
                var tip = await _context.CommissionTips.FindAsync(tipId);
                if (tip == null) return false;

                _context.CommissionTips.Remove(tip);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted tip {TipId}", tipId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tip {TipId}", tipId);
                return false;
            }
        }

        // ========== РАСЧЕТЫ ==========

        public async Task<decimal> CalculateCommissionAsync(string bankName, string category, decimal amount)
        {
            var commissions = await _context.BankCommissions
                .Where(c => c.BankName.ToLower() == bankName.ToLower()
                         && c.Category.ToLower() == category.ToLower()
                         && (c.MinAmount == null || amount >= c.MinAmount)
                         && (c.MaxAmount == null || amount <= c.MaxAmount))
                .ToListAsync();

            if (!commissions.Any())
                return 0;

            var commission = commissions.OrderByDescending(c => c.Priority).First();

            switch (commission.CommissionType)
            {
                case "percent":
                    return amount * (commission.PercentValue ?? 0) / 100;

                case "fixed":
                    return commission.FixedValue ?? 0;

                case "combined":
                    var percent = amount * (commission.PercentValue ?? 0) / 100;
                    return percent + (commission.FixedValue ?? 0);

                default:
                    return 0;
            }
        }

        public async Task<string> GetAdviceForTransactionAsync(string bankName, string category, decimal amount)
        {
            var tips = await GetTipsByBankAsync(bankName);
            var generalTips = await GetTipsByCategoryAsync("general");

            var allTips = tips.Concat(generalTips)
                .OrderByDescending(t => t.Priority)
                .Take(3);

            var advice = new StringBuilder();
            advice.AppendLine($"💡 СОВЕТЫ ДЛЯ {bankName.ToUpper()} - {category}:");
            advice.AppendLine();

            foreach (var tip in allTips)
            {
                advice.AppendLine($"• {tip.Title}");
                advice.AppendLine($"  {tip.Content}");
                advice.AppendLine();
            }

            // Добавляем конкретный расчет
            var commission = await CalculateCommissionAsync(bankName, category, amount);
            if (commission > 0)
            {
                advice.AppendLine($"💰 КОМИССИЯ СОСТАВИТ: {commission:F2} ₽");
            }

            return advice.ToString();
        }
    }
}