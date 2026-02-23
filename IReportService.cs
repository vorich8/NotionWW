using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public interface IReportService
    {
        // CRUD операции
        Task<DbReport> CreateReportAsync(DbReport report);
        Task<DbReport?> GetReportAsync(int reportId);
        Task<List<DbReport>> GetAllReportsAsync();
        Task<List<DbReport>> GetReportsByInvestorAsync(string investorName);
        Task<List<DbReport>> GetReportsByDateRangeAsync(DateTime start, DateTime end);
        Task<bool> UpdateReportAsync(DbReport report);
        Task<bool> DeleteReportAsync(int reportId);

        // Поиск
        Task<List<DbReport>> SearchReportsAsync(string searchTerm);

        // Экспорт
        Task<byte[]> ExportReportToPdfAsync(int reportId);

        // Статистика
        Task<ReportStatistics> GetReportStatisticsAsync();
    }

    public class ReportStatistics
    {
        public int TotalReports { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal TotalDeposits { get; set; }
        public int ReportsThisMonth { get; set; }
    }
}