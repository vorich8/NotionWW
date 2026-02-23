using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamManagerBot.Models;
using ScottPlot;
using ScottPlot.Colormaps;
using System.Drawing;
using ScottPlot.TickGenerators;

namespace TeamManagerBot.Services
{
    public class CryptoService : ICryptoService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CryptoService> _logger;

        public CryptoService(ApplicationDbContext context, ILogger<CryptoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ========== КРУГИ ==========

        public async Task<CryptoCircle> CreateCircleAsync(int circleNumber, decimal depositAmount, decimal expectedEndAmount)
        {
            try
            {
                var circle = new CryptoCircle
                {
                    CircleNumber = circleNumber,
                    DepositAmount = depositAmount,
                    ExpectedEndAmount = expectedEndAmount,
                    StartDate = DateTime.UtcNow,
                    Status = CircleStatus.Active
                };

                _context.CryptoCircles.Add(circle);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created crypto circle #{CircleNumber} with deposit {DepositAmount}",
                    circleNumber, depositAmount);
                return circle;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating crypto circle #{CircleNumber}", circleNumber);
                throw;
            }
        }

        public async Task<CryptoCircle?> GetCircleAsync(int circleId)
        {
            return await _context.CryptoCircles
                .Include(c => c.Deals)
                .FirstOrDefaultAsync(c => c.Id == circleId);
        }

        public async Task<List<CryptoCircle>> GetAllCirclesAsync()
        {
            return await _context.CryptoCircles
                .Include(c => c.Deals)
                .OrderByDescending(c => c.CircleNumber)
                .ToListAsync();
        }

        public async Task<List<CryptoCircle>> GetActiveCirclesAsync()
        {
            return await _context.CryptoCircles
                .Where(c => c.Status == CircleStatus.Active)
                .Include(c => c.Deals)
                .OrderByDescending(c => c.CircleNumber)
                .ToListAsync();
        }

        public async Task<bool> CompleteCircleAsync(int circleId, decimal actualEndAmount)
        {
            try
            {
                var circle = await _context.CryptoCircles.FindAsync(circleId);
                if (circle == null)
                {
                    _logger.LogWarning("Circle {CircleId} not found", circleId);
                    return false;
                }

                circle.ActualEndAmount = actualEndAmount;
                circle.EndDate = DateTime.UtcNow;
                circle.Status = CircleStatus.Completed;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Completed crypto circle #{CircleNumber} with profit {Profit}",
                    circle.CircleNumber, circle.ActualProfit);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing circle {CircleId}", circleId);
                return false;
            }
        }

        public async Task<bool> UpdateCircleAsync(CryptoCircle circle)
        {
            try
            {
                var existing = await _context.CryptoCircles.FindAsync(circle.Id);
                if (existing == null) return false;

                existing.CircleNumber = circle.CircleNumber;
                existing.DepositAmount = circle.DepositAmount;
                existing.ExpectedEndAmount = circle.ExpectedEndAmount;
                existing.ActualEndAmount = circle.ActualEndAmount;
                existing.Status = circle.Status;
                existing.EndDate = circle.EndDate;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated crypto circle #{CircleNumber}", circle.CircleNumber);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating circle {CircleId}", circle.Id);
                return false;
            }
        }

        public async Task<bool> DeleteCircleAsync(int circleId)
        {
            try
            {
                var circle = await _context.CryptoCircles
                    .Include(c => c.Deals)
                    .FirstOrDefaultAsync(c => c.Id == circleId);

                if (circle == null) return false;

                // Удаляем связанные сделки
                _context.CryptoDeals.RemoveRange(circle.Deals);
                _context.CryptoCircles.Remove(circle);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted crypto circle #{CircleNumber}", circle.CircleNumber);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting circle {CircleId}", circleId);
                return false;
            }
        }

        // ========== СДЕЛКИ ==========
        public async Task<List<CryptoDeal>> GetAllDealsAsync()
        {
            return await _context.CryptoDeals
                .Include(d => d.Circle)
                .OrderByDescending(d => d.Date)
                .ToListAsync();
        }
        public async Task<CryptoDeal> CreateDealAsync(int dealNumber, decimal amount, DateTime date, int? circleId = null)
        {
            try
            {
                var deal = new CryptoDeal
                {
                    DealNumber = dealNumber,
                    Amount = amount,
                    Date = date,
                    CircleId = circleId
                };

                _context.CryptoDeals.Add(deal);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created crypto deal #{DealNumber} with amount {Amount}", dealNumber, amount);
                return deal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating crypto deal #{DealNumber}", dealNumber);
                throw;
            }
        }

        public async Task<CryptoDeal?> GetDealAsync(int dealId)
        {
            return await _context.CryptoDeals
                .Include(d => d.Circle)
                .FirstOrDefaultAsync(d => d.Id == dealId);
        }

        public async Task<List<CryptoDeal>> GetDealsByCircleAsync(int circleId)
        {
            return await _context.CryptoDeals
                .Where(d => d.CircleId == circleId)
                .OrderByDescending(d => d.Date)
                .ToListAsync();
        }

        public async Task<List<CryptoDeal>> GetDealsByDateRangeAsync(DateTime start, DateTime end)
        {
            return await _context.CryptoDeals
                .Where(d => d.Date >= start && d.Date <= end)
                .Include(d => d.Circle)
                .OrderByDescending(d => d.Date)
                .ToListAsync();
        }

        public async Task<bool> UpdateDealAsync(CryptoDeal deal)
        {
            try
            {
                var existing = await _context.CryptoDeals.FindAsync(deal.Id);
                if (existing == null) return false;

                existing.DealNumber = deal.DealNumber;
                existing.Amount = deal.Amount;
                existing.Date = deal.Date;
                existing.CircleId = deal.CircleId;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated crypto deal #{DealNumber}", deal.DealNumber);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating deal {DealId}", deal.Id);
                return false;
            }
        }

        public async Task<bool> DeleteDealAsync(int dealId)
        {
            try
            {
                var deal = await _context.CryptoDeals.FindAsync(dealId);
                if (deal == null) return false;

                _context.CryptoDeals.Remove(deal);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted crypto deal #{DealNumber}", deal.DealNumber);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting deal {DealId}", dealId);
                return false;
            }
        }

        // ========== СТАТИСТИКА ==========

        public async Task<CryptoStatistics> GetCryptoStatisticsAsync(DateTime? start = null, DateTime? end = null)
        {
            start ??= DateTime.UtcNow.AddMonths(-1);
            end ??= DateTime.UtcNow;

            var circles = await _context.CryptoCircles
                .Where(c => c.StartDate >= start && c.StartDate <= end)
                .ToListAsync();

            var deals = await _context.CryptoDeals
                .Where(d => d.Date >= start && d.Date <= end)
                .ToListAsync();

            var stats = new CryptoStatistics
            {
                TotalCircles = circles.Count,
                ActiveCircles = circles.Count(c => c.Status == CircleStatus.Active),
                CompletedCircles = circles.Count(c => c.Status == CircleStatus.Completed),
                TotalDeposit = circles.Sum(c => c.DepositAmount),
                TotalExpectedProfit = circles.Sum(c => c.ExpectedProfit),
                TotalActualProfit = circles.Where(c => c.ActualProfit.HasValue).Sum(c => c.ActualProfit ?? 0),
                TotalDeals = deals.Count,
                TotalDealsAmount = deals.Sum(d => d.Amount)
            };

            return stats;
        }

        // ========== ГРАФИК ==========

        public async Task<byte[]> GenerateDealsChartAsync(int months = 6)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddMonths(-months);

            var deals = await _context.CryptoDeals
                .Where(d => d.Date >= startDate && d.Date <= endDate)
                .OrderBy(d => d.Date)
                .ToListAsync();

            if (!deals.Any())
                return Array.Empty<byte>();

            var plt = new Plot();

            double[] xValues;
            double[] yValues;

            if (deals.Count > 30)
            {
                var weekly = deals
                    .GroupBy(d => new { d.Date.Year, Week = GetWeekOfYear(d.Date) })
                    .Select(g => new
                    {
                        Date = new DateTime(g.Key.Year, 1, 1).AddDays((g.Key.Week - 1) * 7),
                        Total = g.Sum(d => (double)d.Amount)
                    })
                    .OrderBy(g => g.Date)
                    .ToList();

                xValues = weekly.Select(w => w.Date.ToOADate()).ToArray();
                yValues = weekly.Select(w => w.Total).ToArray();
            }
            else
            {
                xValues = deals.Select(d => d.Date.ToOADate()).ToArray();
                yValues = deals.Select(d => (double)d.Amount).ToArray();
            }

            var scatter = plt.Add.Scatter(xValues, yValues);
            scatter.LineWidth = 2;
            scatter.Color = new ScottPlot.Color(0, 120, 255);
            scatter.MarkerSize = 5;

            plt.Title("Динамика сделок");
            plt.YLabel("Сумма (USDT)");
            plt.Axes.DateTimeTicksBottom();

            return plt.GetImageBytes(800, 400, ImageFormat.Png);
        }

        public async Task<byte[]> GenerateProfitChartAsync(int months = 6)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddMonths(-months);

            var circles = await _context.CryptoCircles
                .Where(c => c.EndDate >= startDate && c.EndDate <= endDate && c.Status == CircleStatus.Completed)
                .ToListAsync();

            if (!circles.Any())
                return Array.Empty<byte>();

            var plt = new Plot();

            // Группируем по месяцам
            var monthlyData = new Dictionary<string, decimal>();

            foreach (var circle in circles)
            {
                if (circle.EndDate.HasValue && circle.ActualProfit.HasValue)
                {
                    var monthKey = circle.EndDate.Value.ToString("yyyy-MM");
                    if (!monthlyData.ContainsKey(monthKey))
                        monthlyData[monthKey] = 0;

                    monthlyData[monthKey] += circle.ActualProfit.Value;
                }
            }

            var orderedMonths = monthlyData.OrderBy(x => x.Key).ToList();
            var xValues = orderedMonths.Select((_, i) => (double)i).ToArray();
            var yValues = orderedMonths.Select(m => (double)m.Value).ToArray();
            var monthLabels = orderedMonths.Select(m => m.Key).ToArray();

            // Столбчатая диаграмма
            var bars = plt.Add.Bars(xValues, yValues);
            bars.Color = new ScottPlot.Color(40, 167, 69);

            // Настройка осей
            plt.Title("Прибыль по месяцам");
            plt.YLabel("Прибыль (USDT)");

            // Подписи месяцев для ScottPlot 5
            plt.Axes.Bottom.SetTicks(xValues, monthLabels);

            return plt.GetImageBytes(800, 400, ImageFormat.Png);
        }

        public async Task<byte[]> GenerateCirclesChartAsync()
        {
            var circles = await _context.CryptoCircles.ToListAsync();

            if (!circles.Any())
                return Array.Empty<byte>();

            var plt = new Plot();

            // Статистика по кругам
            var activeCount = circles.Count(c => c.Status == CircleStatus.Active);
            var completedCount = circles.Count(c => c.Status == CircleStatus.Completed);
            var cancelledCount = circles.Count(c => c.Status == CircleStatus.Cancelled);

            var values = new List<double>();
            var sliceColors = new List<ScottPlot.Color>();
            var sliceLabels = new List<string>();

            if (activeCount > 0)
            {
                values.Add(activeCount);
                sliceLabels.Add($"Активные ({activeCount})");
                sliceColors.Add(new ScottPlot.Color(0, 123, 255));
            }

            if (completedCount > 0)
            {
                values.Add(completedCount);
                sliceLabels.Add($"Завершённые ({completedCount})");
                sliceColors.Add(new ScottPlot.Color(40, 167, 69));
            }

            if (cancelledCount > 0)
            {
                values.Add(cancelledCount);
                sliceLabels.Add($"Отменённые ({cancelledCount})");
                sliceColors.Add(new ScottPlot.Color(220, 53, 69));
            }

            if (values.Count == 0)
                return Array.Empty<byte>();

            // Создаем круговую диаграмму
            var pie = plt.Add.Pie(values.ToArray());

            // Настройка цветов и подписей
            for (int i = 0; i < sliceColors.Count; i++)
            {
                pie.Slices[i].FillColor = sliceColors[i];
                pie.Slices[i].Label = sliceLabels[i];
                pie.Slices[i].LabelFontSize = 14;
            }

            // Добавляем проценты
            var total = values.Sum();
            for (int i = 0; i < pie.Slices.Count; i++)
            {
                var percent = (values[i] / total * 100).ToString("F1");
                pie.Slices[i].Label += $"\n{percent}%";
            }

            // Отделяем куски (explode)
            pie.ExplodeFraction = 0.05; 

            plt.Title("Статусы кругов");

            // Убираем сетку
            plt.Grid.IsVisible = false;

            // Убираем оси и рамку
            plt.Axes.Left.IsVisible = false;
            plt.Axes.Bottom.IsVisible = false;
            plt.Axes.Right.IsVisible = false;
            plt.Axes.Top.IsVisible = false;

            return plt.GetImageBytes(600, 400, ImageFormat.Png);
        }

        public async Task<byte[]> GenerateMonthlyDealsChartAsync(int months = 6)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddMonths(-months);

            var deals = await _context.CryptoDeals
                .Where(d => d.Date >= startDate && d.Date <= endDate)
                .OrderBy(d => d.Date)
                .ToListAsync();

            if (!deals.Any())
                return Array.Empty<byte>();

            var plt = new Plot();

            // Группируем по месяцам
            var monthlyData = deals
                .GroupBy(d => new { d.Date.Year, d.Date.Month })
                .Select(g => new
                {
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1),
                    Total = g.Sum(d => (double)d.Amount),
                    Count = g.Count()
                })
                .OrderBy(g => g.Month)
                .ToList();

            var xValues = monthlyData.Select((_, i) => (double)i).ToArray();
            var yValues = monthlyData.Select(m => m.Total).ToArray();
            var monthLabels = monthlyData.Select(m => m.Month.ToString("MMM yyyy")).ToArray();

            // Столбчатая диаграмма
            var bars = plt.Add.Bars(xValues, yValues);
            bars.Color = new ScottPlot.Color(0, 123, 255);

            // Добавим подписи количества сделок
            for (int i = 0; i < monthlyData.Count; i++)
            {
                var text = plt.Add.Text(monthlyData[i].Count.ToString(), xValues[i], yValues[i] + 5);
                text.LabelFontColor = new ScottPlot.Color(0, 0, 0);
                text.LabelFontSize = 10;
                text.LabelAlignment = Alignment.LowerCenter;
            }

            plt.Title("Объём сделок по месяцам");
            plt.YLabel("Сумма (USDT)");

            // Подписи месяцев
            plt.Axes.Bottom.SetTicks(xValues, monthLabels);
            plt.Axes.Bottom.TickLabelStyle.Rotation = 45;

            return plt.GetImageBytes(900, 400, ImageFormat.Png);
        }

        private int GetWeekOfYear(DateTime date)
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            return culture.Calendar.GetWeekOfYear(date,
                System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                DayOfWeek.Monday);
        }
    }
}