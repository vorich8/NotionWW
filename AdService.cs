using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public class AdService : IAdService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdService> _logger;

        public AdService(ApplicationDbContext context, ILogger<AdService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DbAd> CreateAdAsync(DbAd ad)
        {
            try
            {
                ad.CreatedAt = DateTime.UtcNow;
                _context.DbAds.Add(ad);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created ad campaign: {CampaignName}", ad.CampaignName);
                return ad;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ad campaign");
                throw;
            }
        }

        public async Task<DbAd?> GetAdAsync(int adId)
        {
            return await _context.DbAds.FindAsync(adId);
        }

        public async Task<List<DbAd>> GetAllAdsAsync()
        {
            return await _context.DbAds
                .OrderByDescending(a => a.StartDate)
                .ToListAsync();
        }

        public async Task<List<DbAd>> GetAdsByProjectAsync(string projectName)
        {
            return await _context.DbAds
                .Where(a => a.ProjectName == projectName)
                .OrderByDescending(a => a.StartDate)
                .ToListAsync();
        }

        public async Task<List<DbAd>> GetAdsByStatusAsync(string status)
        {
            return await _context.DbAds
                .Where(a => a.Status == status)
                .OrderByDescending(a => a.StartDate)
                .ToListAsync();
        }

        public async Task<List<DbAd>> GetAdsByDateRangeAsync(DateTime start, DateTime end)
        {
            return await _context.DbAds
                .Where(a => a.StartDate >= start && a.StartDate <= end)
                .OrderByDescending(a => a.StartDate)
                .ToListAsync();
        }

        public async Task<bool> UpdateAdAsync(DbAd ad)
        {
            try
            {
                var existing = await _context.DbAds.FindAsync(ad.Id);
                if (existing == null) return false;

                existing.CampaignName = ad.CampaignName;
                existing.ProjectName = ad.ProjectName;
                existing.Budget = ad.Budget;
                existing.Spent = ad.Spent;
                existing.StartDate = ad.StartDate;
                existing.EndDate = ad.EndDate;
                existing.Description = ad.Description;
                existing.Results = ad.Results;
                existing.PostLink = ad.PostLink;
                existing.Status = ad.Status;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated ad campaign: {CampaignName}", ad.CampaignName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ad campaign {AdId}", ad.Id);
                return false;
            }
        }

        public async Task<bool> DeleteAdAsync(int adId)
        {
            try
            {
                var ad = await _context.DbAds.FindAsync(adId);
                if (ad == null) return false;

                _context.DbAds.Remove(ad);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted ad campaign: {CampaignName}", ad.CampaignName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ad campaign {AdId}", adId);
                return false;
            }
        }

        public async Task<bool> AddSpentAsync(int adId, decimal amount)
        {
            try
            {
                var ad = await _context.DbAds.FindAsync(adId);
                if (ad == null) return false;

                ad.Spent = (ad.Spent ?? 0) + amount;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Added spent {Amount} to campaign {CampaignName}", amount, ad.CampaignName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding spent to campaign {AdId}", adId);
                return false;
            }
        }

        public async Task<List<DbAd>> SearchAdsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<DbAd>();

            searchTerm = searchTerm.ToLower();
            return await _context.DbAds
                .Where(a => a.CampaignName.ToLower().Contains(searchTerm) ||
                            (a.Description != null && a.Description.ToLower().Contains(searchTerm)) ||
                            (a.ProjectName != null && a.ProjectName.ToLower().Contains(searchTerm)))
                .OrderByDescending(a => a.StartDate)
                .Take(20)
                .ToListAsync();
        }

        public async Task<AdStatistics> GetAdStatisticsAsync()
        {
            var ads = await _context.DbAds.ToListAsync();

            return new AdStatistics
            {
                TotalCampaigns = ads.Count,
                ActiveCampaigns = ads.Count(a => a.Status == "Активна"),
                TotalBudget = ads.Sum(a => a.Budget),
                TotalSpent = ads.Sum(a => a.Spent ?? 0),
                SpentByProject = ads
                    .Where(a => !string.IsNullOrEmpty(a.ProjectName) && a.Spent.HasValue)
                    .GroupBy(a => a.ProjectName!)
                    .ToDictionary(g => g.Key, g => g.Sum(a => a.Spent ?? 0))
            };
        }
    }
}