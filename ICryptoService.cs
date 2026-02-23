using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public interface ICryptoService
    {
        // Круги
        Task<CryptoCircle> CreateCircleAsync(int circleNumber, decimal depositAmount, decimal expectedEndAmount);
        Task<CryptoCircle?> GetCircleAsync(int circleId);
        Task<List<CryptoCircle>> GetAllCirclesAsync();
        Task<List<CryptoCircle>> GetActiveCirclesAsync();
        Task<bool> CompleteCircleAsync(int circleId, decimal actualEndAmount);
        Task<bool> UpdateCircleAsync(CryptoCircle circle);
        Task<bool> DeleteCircleAsync(int circleId);

        // Сделки
        Task<CryptoDeal> CreateDealAsync(int dealNumber, decimal amount, DateTime date, int? circleId = null);
        Task<CryptoDeal?> GetDealAsync(int dealId);
        Task<List<CryptoDeal>> GetDealsByCircleAsync(int circleId);
        Task<List<CryptoDeal>> GetDealsByDateRangeAsync(DateTime start, DateTime end);
        Task<bool> UpdateDealAsync(CryptoDeal deal);
        Task<bool> DeleteDealAsync(int dealId);
        Task<List<CryptoDeal>> GetAllDealsAsync();

        // Статистика
        Task<CryptoStatistics> GetCryptoStatisticsAsync(DateTime? start = null, DateTime? end = null);

        // График
        Task<byte[]> GenerateDealsChartAsync(int months = 6);
        Task<byte[]> GenerateProfitChartAsync(int months = 6);
        Task<byte[]> GenerateCirclesChartAsync();
    }

    public class CryptoStatistics
    {
        public int TotalCircles { get; set; }
        public int ActiveCircles { get; set; }
        public int CompletedCircles { get; set; }
        public decimal TotalDeposit { get; set; }
        public decimal TotalExpectedProfit { get; set; }
        public decimal TotalActualProfit { get; set; }
        public decimal TotalDealsAmount { get; set; }
        public int TotalDeals { get; set; }
    }
}