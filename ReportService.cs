using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using TeamManagerBot.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TeamManagerBot.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportService> _logger;

        public ReportService(ApplicationDbContext context, ILogger<ReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DbReport> CreateReportAsync(DbReport report)
        {
            try
            {
                report.CreatedAt = DateTime.UtcNow;
                _context.DbReports.Add(report);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created report: {Title}", report.Title);
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report");
                throw;
            }
        }

        public async Task<DbReport?> GetReportAsync(int reportId)
        {
            return await _context.DbReports.FindAsync(reportId);
        }

        public async Task<List<DbReport>> GetAllReportsAsync()
        {
            return await _context.DbReports
                .OrderByDescending(r => r.ReportDate)
                .ToListAsync();
        }

        public async Task<List<DbReport>> GetReportsByInvestorAsync(string investorName)
        {
            return await _context.DbReports
                .Where(r => r.InvestorName == investorName)
                .OrderByDescending(r => r.ReportDate)
                .ToListAsync();
        }

        public async Task<List<DbReport>> GetReportsByDateRangeAsync(DateTime start, DateTime end)
        {
            return await _context.DbReports
                .Where(r => r.ReportDate >= start && r.ReportDate <= end)
                .OrderByDescending(r => r.ReportDate)
                .ToListAsync();
        }

        public async Task<bool> UpdateReportAsync(DbReport report)
        {
            try
            {
                var existing = await _context.DbReports.FindAsync(report.Id);
                if (existing == null) return false;

                existing.Title = report.Title;
                existing.InvestorName = report.InvestorName;
                existing.ReportDate = report.ReportDate;
                existing.TotalProfit = report.TotalProfit;
                existing.TotalDeposit = report.TotalDeposit;
                existing.Summary = report.Summary;
                existing.FilePath = report.FilePath;
                existing.Status = report.Status;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated report: {Title}", report.Title);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating report {ReportId}", report.Id);
                return false;
            }
        }

        public async Task<bool> DeleteReportAsync(int reportId)
        {
            try
            {
                var report = await _context.DbReports.FindAsync(reportId);
                if (report == null) return false;

                _context.DbReports.Remove(report);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted report: {Title}", report.Title);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report {ReportId}", reportId);
                return false;
            }
        }

        public async Task<List<DbReport>> SearchReportsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<DbReport>();

            searchTerm = searchTerm.ToLower();
            return await _context.DbReports
                .Where(r => r.Title.ToLower().Contains(searchTerm) ||
                            (r.InvestorName != null && r.InvestorName.ToLower().Contains(searchTerm)) ||
                            (r.Summary != null && r.Summary.ToLower().Contains(searchTerm)))
                .OrderByDescending(r => r.ReportDate)
                .Take(20)
                .ToListAsync();
        }

        public async Task<byte[]> ExportReportToPdfAsync(int reportId)
        {
            var report = await _context.DbReports.FindAsync(reportId);
            if (report == null)
                return Array.Empty<byte>();

            // ВАЖНО: Инициализация QuestPDF (достаточно один раз при запуске, но можно и здесь)
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text($"ОТЧЁТ: {report.Title}")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            // Дата создания
                            column.Item().Text($"Дата создания: {report.CreatedAt:dd.MM.yyyy HH:mm}")
                                .FontSize(10).Italic();

                            column.Item().PaddingTop(20).Text("ИНФОРМАЦИЯ ОБ ИНВЕСТОРЕ:")
                                .SemiBold().FontSize(14).FontColor(Colors.Green.Darken2);

                            column.Item().PaddingLeft(20).Text($"Инвестор: {report.InvestorName ?? "Не указан"}");
                            column.Item().PaddingLeft(20).Text($"Дата отчёта: {report.ReportDate:dd.MM.yyyy}");

                            column.Item().PaddingTop(20).Text("ФИНАНСОВЫЕ ПОКАЗАТЕЛИ:")
                                .SemiBold().FontSize(14).FontColor(Colors.Green.Darken2);

                            column.Item().PaddingLeft(20).Text($"Депозит: {report.TotalDeposit:N0} ₽");
                            column.Item().PaddingLeft(20).Text($"Прибыль: {report.TotalProfit:N0} ₽");

                            var total = (report.TotalDeposit ?? 0) + (report.TotalProfit ?? 0);
                            column.Item().PaddingLeft(20).Text($"Итого: {total:N0} ₽")
                                .SemiBold().FontSize(13);

                            column.Item().PaddingTop(20).Text("СОДЕРЖАНИЕ:")
                                .SemiBold().FontSize(14).FontColor(Colors.Green.Darken2);

                            column.Item().PaddingLeft(20).Text(report.Summary ?? "Нет данных");

                            column.Item().PaddingTop(20).Text($"Статус: {report.Status ?? "Черновик"}")
                                .SemiBold().FontSize(12).FontColor(Colors.Orange.Darken2);
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Сгенерировано ");
                            x.Span(DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm")).SemiBold();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<ReportStatistics> GetReportStatisticsAsync()
        {
            var reports = await _context.DbReports.ToListAsync();
            var thisMonth = DateTime.UtcNow.Month;

            return new ReportStatistics
            {
                TotalReports = reports.Count,
                TotalProfit = reports.Sum(r => r.TotalProfit ?? 0),
                TotalDeposits = reports.Sum(r => r.TotalDeposit ?? 0),
                ReportsThisMonth = reports.Count(r => r.ReportDate.Month == thisMonth)
            };
        }
    }
}