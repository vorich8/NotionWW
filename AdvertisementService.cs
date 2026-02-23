using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public class AdvertisementService : IAdvertisementService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdvertisementService> _logger;

        public AdvertisementService(ApplicationDbContext context, ILogger<AdvertisementService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AdCampaign> CreateCampaignAsync(string campaignName, string projectName, decimal budget,
            string? postContent, DateTime startDate)
        {
            try
            {
                var campaign = new AdCampaign
                {
                    CampaignName = campaignName,
                    ProjectName = projectName,
                    Budget = budget,
                    PostContent = postContent,
                    StartDate = startDate,
                    Spent = 0
                };

                _context.AdCampaigns.Add(campaign);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created ad campaign '{CampaignName}' for project {ProjectName}",
                    campaignName, projectName);
                return campaign;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ad campaign '{CampaignName}'", campaignName);
                throw;
            }
        }

        public async Task<AdCampaign?> GetCampaignAsync(int campaignId)
        {
            return await _context.AdCampaigns.FindAsync(campaignId);
        }

        public async Task<List<AdCampaign>> GetCampaignsByProjectAsync(string projectName)
        {
            return await _context.AdCampaigns
                .Where(c => c.ProjectName == projectName)
                .OrderByDescending(c => c.StartDate)
                .ToListAsync();
        }

        public async Task<List<AdCampaign>> GetCampaignsByDateRangeAsync(DateTime start, DateTime end)
        {
            return await _context.AdCampaigns
                .Where(c => c.StartDate >= start && c.StartDate <= end)
                .OrderByDescending(c => c.StartDate)
                .ToListAsync();
        }

        public async Task<List<AdCampaign>> GetAllCampaignsAsync()
        {
            return await _context.AdCampaigns
                .OrderByDescending(c => c.StartDate)
                .ToListAsync();
        }

        public async Task<bool> UpdateCampaignAsync(AdCampaign campaign)
        {
            try
            {
                var existing = await _context.AdCampaigns.FindAsync(campaign.Id);
                if (existing == null) return false;

                existing.CampaignName = campaign.CampaignName;
                existing.ProjectName = campaign.ProjectName;
                existing.Budget = campaign.Budget;
                existing.Spent = campaign.Spent;
                existing.PostContent = campaign.PostContent;
                existing.StartDate = campaign.StartDate;
                existing.EndDate = campaign.EndDate;
                existing.Results = campaign.Results;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated ad campaign {CampaignId}: '{CampaignName}'",
                    campaign.Id, campaign.CampaignName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ad campaign {CampaignId}", campaign.Id);
                return false;
            }
        }

        public async Task<bool> AddSpentAsync(int campaignId, decimal amount)
        {
            try
            {
                var campaign = await _context.AdCampaigns.FindAsync(campaignId);
                if (campaign == null) return false;

                campaign.Spent = (campaign.Spent ?? 0) + amount;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Added spent {Amount} to campaign {CampaignId}", amount, campaignId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding spent to campaign {CampaignId}", campaignId);
                return false;
            }
        }

        public async Task<bool> CompleteCampaignAsync(int campaignId, string results)
        {
            try
            {
                var campaign = await _context.AdCampaigns.FindAsync(campaignId);
                if (campaign == null) return false;

                campaign.EndDate = DateTime.UtcNow;
                campaign.Results = results;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Completed ad campaign {CampaignId} with results: {Results}",
                    campaignId, results);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing ad campaign {CampaignId}", campaignId);
                return false;
            }
        }

        public async Task<bool> DeleteCampaignAsync(int campaignId)
        {
            try
            {
                var campaign = await _context.AdCampaigns.FindAsync(campaignId);
                if (campaign == null) return false;

                _context.AdCampaigns.Remove(campaign);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted ad campaign {CampaignId}", campaignId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ad campaign {CampaignId}", campaignId);
                return false;
            }
        }

        public async Task<bool> AddScreenshotAsync(int campaignId, string screenshotUrl)
        {
            try
            {
                var campaign = await _context.AdCampaigns.FindAsync(campaignId);
                if (campaign == null) return false;

                if (campaign.Screenshots == null)
                    campaign.Screenshots = new List<string>();

                campaign.Screenshots.Add(screenshotUrl);

                // Сериализуем список в JSON для хранения
                var screenshotsJson = JsonSerializer.Serialize(campaign.Screenshots);
                // Здесь нужно либо добавить поле в модель, либо использовать отдельную таблицу
                // Пока оставим комментарий

                await _context.SaveChangesAsync();
                _logger.LogInformation("Added screenshot to campaign {CampaignId}", campaignId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding screenshot to campaign {CampaignId}", campaignId);
                return false;
            }
        }

        public async Task<AdStatistics> GetAdStatisticsAsync(DateTime? start = null, DateTime? end = null)
        {
            start ??= DateTime.UtcNow.AddMonths(-1);
            end ??= DateTime.UtcNow;

            var campaigns = await _context.AdCampaigns
                .Where(c => c.StartDate >= start && c.StartDate <= end)
                .ToListAsync();

            var stats = new AdStatistics
            {
                TotalCampaigns = campaigns.Count,
                ActiveCampaigns = campaigns.Count(c => !c.EndDate.HasValue),
                TotalBudget = campaigns.Sum(c => c.Budget),
                TotalSpent = campaigns.Sum(c => c.Spent ?? 0),
                SpentByProject = campaigns
                    .Where(c => c.Spent.HasValue)
                    .GroupBy(c => c.ProjectName)
                    .ToDictionary(g => g.Key, g => g.Sum(c => c.Spent ?? 0)),
                CampaignsByProject = campaigns
                    .GroupBy(c => c.ProjectName)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return stats;
        }
    }
}