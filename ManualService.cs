using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public class ManualService : IManualService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ManualService> _logger;

        public ManualService(ApplicationDbContext context, ILogger<ManualService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DbManual> CreateManualAsync(DbManual manual)
        {
            try
            {
                manual.CreatedAt = DateTime.UtcNow;
                _context.DbManuals.Add(manual);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created manual: {Title}", manual.Title);
                return manual;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating manual");
                throw;
            }
        }

        public async Task<DbManual?> GetManualAsync(int manualId)
        {
            return await _context.DbManuals.FindAsync(manualId);
        }

        public async Task<List<DbManual>> GetAllManualsAsync()
        {
            return await _context.DbManuals
                .OrderBy(m => m.Category)
                .ThenBy(m => m.BankName)
                .ToListAsync();
        }

        public async Task<List<DbManual>> GetManualsByCategoryAsync(string category)
        {
            return await _context.DbManuals
                .Where(m => m.Category == category)
                .OrderBy(m => m.BankName)
                .ToListAsync();
        }

        public async Task<List<DbManual>> GetManualsByBankAsync(string bankName)
        {
            return await _context.DbManuals
                .Where(m => m.BankName == bankName)
                .OrderBy(m => m.Category)
                .ToListAsync();
        }

        public async Task<bool> UpdateManualAsync(DbManual manual)
        {
            try
            {
                var existing = await _context.DbManuals.FindAsync(manual.Id);
                if (existing == null) return false;

                existing.Title = manual.Title;
                existing.BankName = manual.BankName;
                existing.Category = manual.Category;
                existing.Content = manual.Content;
                existing.FilePath = manual.FilePath;
                existing.Author = manual.Author;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated manual: {Title}", manual.Title);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating manual {ManualId}", manual.Id);
                return false;
            }
        }

        public async Task<bool> DeleteManualAsync(int manualId)
        {
            try
            {
                var manual = await _context.DbManuals.FindAsync(manualId);
                if (manual == null) return false;

                _context.DbManuals.Remove(manual);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted manual: {Title}", manual.Title);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting manual {ManualId}", manualId);
                return false;
            }
        }

        public async Task<List<DbManual>> SearchManualsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<DbManual>();

            searchTerm = searchTerm.ToLower();
            return await _context.DbManuals
                .Where(m => m.Title.ToLower().Contains(searchTerm) ||
                            m.Content.ToLower().Contains(searchTerm) ||
                            (m.BankName != null && m.BankName.ToLower().Contains(searchTerm)))
                .OrderBy(m => m.Category)
                .Take(20)
                .ToListAsync();
        }

        public async Task<ManualStatistics> GetManualStatisticsAsync()
        {
            var manuals = await _context.DbManuals.ToListAsync();

            return new ManualStatistics
            {
                TotalManuals = manuals.Count,
                ManualsByCategory = manuals
                    .GroupBy(m => m.Category)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ManualsByBank = manuals
                    .Where(m => !string.IsNullOrEmpty(m.BankName))
                    .GroupBy(m => m.BankName!)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }
    }
}