using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public interface IAdvertisementService
    {
        Task<AdCampaign> CreateCampaignAsync(string campaignName, string projectName, decimal budget,
            string? postContent, DateTime startDate);
        Task<AdCampaign?> GetCampaignAsync(int campaignId);
        Task<List<AdCampaign>> GetCampaignsByProjectAsync(string projectName);
        Task<List<AdCampaign>> GetCampaignsByDateRangeAsync(DateTime start, DateTime end);
        Task<List<AdCampaign>> GetAllCampaignsAsync();
        Task<bool> UpdateCampaignAsync(AdCampaign campaign);
        Task<bool> AddSpentAsync(int campaignId, decimal amount);
        Task<bool> CompleteCampaignAsync(int campaignId, string results);
        Task<bool> DeleteCampaignAsync(int campaignId);
        Task<bool> AddScreenshotAsync(int campaignId, string screenshotUrl);

        // Статистика
        Task<AdStatistics> GetAdStatisticsAsync(DateTime? start = null, DateTime? end = null);
    }

    public class AdStatistics
    {
        public int TotalCampaigns { get; set; }
        public int ActiveCampaigns { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal TotalSpent { get; set; }
        public Dictionary<string, decimal> SpentByProject { get; set; } = new();
        public Dictionary<string, int> CampaignsByProject { get; set; } = new();
    }
}