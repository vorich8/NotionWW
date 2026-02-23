using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TeamManagerBot.Models;
using ScottPlot;
using ScottPlot.TickGenerators;
using System.Drawing;
using System.Drawing.Imaging;

namespace TeamManagerBot.Services
{
    public class FinanceService : IFinanceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FinanceService> _logger;

        public FinanceService(ApplicationDbContext context, ILogger<FinanceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ========== ОСНОВНЫЕ МЕТОДЫ СОЗДАНИЯ ==========

        public async Task<FinancialRecord?> CreateFinancialRecordAsync(
            FinancialRecordType type,
            string category,
            string description,
            decimal amount,
            string currency,
            string? source,
            long? userId,
            int? projectId,
            IFinanceService.FundStatus fundStatus = IFinanceService.FundStatus.Working,
            int? contactId = null,
            decimal? commission = null,
            string? commissionPaidBy = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var record = new FinancialRecord
                {
                    Type = type,
                    Category = category.Trim(),
                    Description = description.Trim(),
                    Amount = amount,
                    Currency = currency.ToUpper(),
                    Source = source?.Trim(),
                    UserId = userId,
                    ProjectId = projectId,
                    TransactionDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    FundStatus = fundStatus.ToString(),
                    ContactId = contactId,
                    Commission = commission,
                    CommissionPaidBy = commissionPaidBy,
                    Metadata = new Dictionary<string, string>
                    {
                        ["created_by"] = userId?.ToString() ?? "system",
                        ["created_at"] = DateTime.UtcNow.ToString("o")
                    }
                };

                // Проверяем пользователя
                if (userId.HasValue)
                {
                    var user = await _context.Users.FindAsync(userId.Value);
                    if (user == null)
                    {
                        _logger.LogWarning("User {UserId} not found", userId.Value);
                        await transaction.RollbackAsync();
                        return null;
                    }
                }

                // Проверяем контакт если указан
                if (contactId.HasValue)
                {
                    var contact = await _context.Contacts.FindAsync(contactId.Value);
                    if (contact == null)
                    {
                        _logger.LogWarning("Contact {ContactId} not found", contactId.Value);
                        await transaction.RollbackAsync();
                        return null;
                    }
                }

                // Проверяем проект
                if (projectId.HasValue)
                {
                    var project = await _context.Projects.FindAsync(projectId.Value);
                    if (project == null)
                    {
                        _logger.LogWarning("Project {ProjectId} not found", projectId.Value);
                        await transaction.RollbackAsync();
                        return null;
                    }
                }

                _context.FinancialRecords.Add(record);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Created financial record: {Type} {Amount} {Currency} - {Category} (Status: {Status})",
                    type, amount, currency, category, fundStatus);
                return record;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating financial record: {Category} {Amount}", category, amount);
                return null;
            }
        }

        public async Task<FinancialRecord?> CreateCommissionRecordAsync(
            decimal amount,
            string currency,
            string description,
            int? projectId,
            long? userId,
            string commissionType = "transfer")
        {
            return await CreateFinancialRecordAsync(
                type: FinancialRecordType.Expense,
                category: "Комиссии",
                description: $"Комиссия ({commissionType}): {description}",
                amount: amount,
                currency: currency,
                source: "commission",
                userId: userId,
                projectId: projectId,
                fundStatus: IFinanceService.FundStatus.Reserved
            );
        }

        // ========== МЕТОДЫ ПОЛУЧЕНИЯ ЗАПИСЕЙ ==========

        public async Task<List<FinancialRecord>> GetAllRecordsAsync()
        {
            return await _context.FinancialRecords
                .Include(f => f.User)
                .Include(f => f.Project)
                .Include(f => f.Contact)
                .OrderByDescending(f => f.TransactionDate)
                .Take(100)
                .ToListAsync();
        }

        public async Task<List<FinancialRecord>> GetRecordsByTypeAsync(FinancialRecordType type)
        {
            return await _context.FinancialRecords
                .Where(f => f.Type == type)
                .Include(f => f.User)
                .Include(f => f.Project)
                .Include(f => f.Contact)
                .OrderByDescending(f => f.TransactionDate)
                .Take(100)
                .ToListAsync();
        }

        public async Task<List<FinancialRecord>> GetRecordsByCategoryAsync(string category)
        {
            return await _context.FinancialRecords
                .Where(f => f.Category != null && f.Category.Trim().ToLower() == category.Trim().ToLower())
                .Include(f => f.User)
                .Include(f => f.Project)
                .Include(f => f.Contact)
                .OrderByDescending(f => f.TransactionDate)
                .ToListAsync(); 
        }

        public async Task<List<FinancialRecord>> GetRecordsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.FinancialRecords
                .Where(f => f.TransactionDate >= startDate && f.TransactionDate <= endDate)
                .Include(f => f.User)
                .Include(f => f.Project)
                .Include(f => f.Contact)
                .OrderByDescending(f => f.TransactionDate)
                .Take(200)
                .ToListAsync();
        }

        public async Task<FinancialRecord?> GetRecordAsync(int recordId)
        {
            return await _context.FinancialRecords
                .Include(f => f.User)
                .Include(f => f.Project)
                .Include(f => f.Contact)
                .FirstOrDefaultAsync(f => f.Id == recordId);
        }

        public async Task<List<FinancialRecord>> GetRecordsByContactAsync(int contactId)
        {
            return await _context.FinancialRecords
                .Where(f => f.ContactId == contactId)
                .Include(f => f.Project)
                .OrderByDescending(f => f.TransactionDate)
                .ToListAsync();
        }

        public async Task<List<FinancialRecord>> GetRecordsByProjectAsync(int projectId)
        {
            return await _context.FinancialRecords
                .Where(f => f.ProjectId == projectId)
                .Include(f => f.User)
                .Include(f => f.Contact)
                .OrderByDescending(f => f.TransactionDate)
                .ToListAsync();
        }

        public async Task<List<FinancialRecord>> GetRecordsByUserAsync(long userId)
        {
            return await _context.FinancialRecords
                .Where(f => f.UserId == userId)
                .Include(f => f.Project)
                .Include(f => f.Contact)
                .OrderByDescending(f => f.TransactionDate)
                .ToListAsync();
        }

        // ========== МЕТОДЫ ОБНОВЛЕНИЯ И УДАЛЕНИЯ ==========

        public async Task<bool> UpdateRecordAsync(FinancialRecord record)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var existingRecord = await _context.FinancialRecords.FindAsync(record.Id);
                if (existingRecord == null)
                {
                    _logger.LogWarning("Financial record {RecordId} not found for update", record.Id);
                    return false;
                }

                // Обновляем поля
                existingRecord.Category = record.Category;
                existingRecord.Description = record.Description;
                existingRecord.Amount = record.Amount;
                existingRecord.Currency = record.Currency;
                existingRecord.Source = record.Source;
                existingRecord.TransactionDate = record.TransactionDate;
                existingRecord.FundStatus = record.FundStatus;
                existingRecord.ContactId = record.ContactId;
                existingRecord.Commission = record.Commission;
                existingRecord.CommissionPaidBy = record.CommissionPaidBy;
                existingRecord.IsConfirmed = record.IsConfirmed;
                existingRecord.TransactionHash = record.TransactionHash;

                // Обновляем метаданные
                if (record.Metadata != null)
                {
                    existingRecord.Metadata = record.Metadata;
                    existingRecord.Metadata["updated_at"] = DateTime.UtcNow.ToString("o");
                    existingRecord.Metadata["updated_by"] = record.UserId?.ToString() ?? "system";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Updated financial record {RecordId}", record.Id);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating financial record {RecordId}", record.Id);
                return false;
            }
        }

        public async Task<bool> DeleteRecordAsync(int recordId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var record = await _context.FinancialRecords.FindAsync(recordId);
                if (record == null)
                {
                    _logger.LogWarning("Financial record {RecordId} not found for deletion", recordId);
                    return false;
                }

                _context.FinancialRecords.Remove(record);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Deleted financial record {RecordId}", recordId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting financial record {RecordId}", recordId);
                return false;
            }
        }

        // ========== МЕТОДЫ УПРАВЛЕНИЯ СТАТУСАМИ ==========

        public async Task<Dictionary<IFinanceService.FundStatus, decimal>> GetBalanceByStatusAsync(string? currency = null)
        {
            var query = _context.FinancialRecords.AsQueryable();

            if (!string.IsNullOrEmpty(currency))
                query = query.Where(f => f.Currency == currency.ToUpper());

            var records = await query.ToListAsync();

            var result = new Dictionary<IFinanceService.FundStatus, decimal>();

            foreach (IFinanceService.FundStatus status in Enum.GetValues(typeof(IFinanceService.FundStatus)))
            {
                var statusStr = status.ToString();
                var income = records
                    .Where(r => r.Type == FinancialRecordType.Income && r.FundStatus == statusStr)
                    .Sum(r => r.Amount);
                var expense = records
                    .Where(r => r.Type == FinancialRecordType.Expense && r.FundStatus == statusStr)
                    .Sum(r => r.Amount);

                result[status] = income - expense;
            }

            return result;
        }

        public async Task<bool> TransferFundsBetweenStatusesAsync(
            int recordId,
            IFinanceService.FundStatus newStatus,
            string reason,
            long? userId = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var record = await _context.FinancialRecords.FindAsync(recordId);
                if (record == null)
                    return false;

                var oldStatus = record.FundStatus;
                record.FundStatus = newStatus.ToString();

                record.Metadata ??= new Dictionary<string, string>();
                record.Metadata[$"status_changed_{DateTime.UtcNow:yyyyMMddHHmmss}"] =
                    $"{oldStatus}->{newStatus}: {reason} (by user: {userId})";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Fund {RecordId} status changed: {OldStatus} -> {NewStatus}",
                    recordId, oldStatus, newStatus);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error changing fund status for record {RecordId}", recordId);
                return false;
            }
        }

        public async Task<bool> UpdateFundStatusAsync(int recordId, IFinanceService.FundStatus newStatus)
        {
            try
            {
                var record = await _context.FinancialRecords.FindAsync(recordId);
                if (record == null)
                    return false;

                record.FundStatus = newStatus.ToString();
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated fund status for record {RecordId} to {NewStatus}", recordId, newStatus);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating fund status for record {RecordId}", recordId);
                return false;
            }
        }

        // ========== МЕТОДЫ ПОЛУЧЕНИЯ БАЛАНСОВ ==========

        public async Task<decimal> GetTotalBalanceAsync()
        {
            var incomes = await _context.FinancialRecords
                .Where(f => f.Type == FinancialRecordType.Income)
                .SumAsync(f => f.Amount);

            var expenses = await _context.FinancialRecords
                .Where(f => f.Type == FinancialRecordType.Expense)
                .SumAsync(f => f.Amount);

            return incomes - expenses;
        }

        public async Task<decimal> GetTotalIncomeAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.FinancialRecords.Where(f => f.Type == FinancialRecordType.Income);

            if (startDate.HasValue)
                query = query.Where(f => f.TransactionDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(f => f.TransactionDate <= endDate.Value);

            return await query.SumAsync(f => f.Amount);
        }

        public async Task<decimal> GetTotalExpensesAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.FinancialRecords.Where(f => f.Type == FinancialRecordType.Expense);

            if (startDate.HasValue)
                query = query.Where(f => f.TransactionDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(f => f.TransactionDate <= endDate.Value);

            return await query.SumAsync(f => f.Amount);
        }

        public async Task<decimal> GetBalanceByDateAsync(DateTime date)
        {
            var incomes = await _context.FinancialRecords
                .Where(f => f.Type == FinancialRecordType.Income && f.TransactionDate.Date <= date.Date)
                .SumAsync(f => f.Amount);

            var expenses = await _context.FinancialRecords
                .Where(f => f.Type == FinancialRecordType.Expense && f.TransactionDate.Date <= date.Date)
                .SumAsync(f => f.Amount);

            return incomes - expenses;
        }

        // ========== МЕТОДЫ СТАТИСТИКИ ==========

        public async Task<byte[]> GenerateMonthlyExpensesChartAsync(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var expenses = await _context.FinancialRecords
                .Where(e => e.Type == FinancialRecordType.Expense &&
                            e.TransactionDate >= startDate &&
                            e.TransactionDate <= endDate)
                .ToListAsync();

            if (!expenses.Any())
                return Array.Empty<byte>();

            // Получаем все уникальные категории за этот месяц
            var categories = expenses.Select(e => e.Category).Distinct().ToList();

            // Группируем по дням
            var dailyData = expenses
                .GroupBy(e => e.TransactionDate.Day)
                .Select(g => new
                {
                    Day = g.Key,
                    ByCategory = g.GroupBy(c => c.Category)
                                 .ToDictionary(cat => cat.Key, cat => cat.Sum(x => (double)x.Amount))
                })
                .OrderBy(x => x.Day)
                .ToList();

            var plt = new Plot();

            // Цветовая палитра для категорий
            var colors = new ScottPlot.Color[]
            {
        new(255, 99, 71),   // Томатный
        new(54, 162, 235),  // Синий
        new(255, 206, 86),  // Жёлтый
        new(75, 192, 192),  // Бирюзовый
        new(153, 102, 255), // Фиолетовый
        new(255, 159, 64),  // Оранжевый
        new(201, 203, 207), // Серый
        new(255, 99, 132),  // Розовый
            };

            // Данные для графика
            double[] dayPositions = dailyData.Select(d => (double)d.Day).ToArray();

            // Создаём стековые столбцы для каждой категории
            double[] bottoms = new double[dayPositions.Length];

            for (int i = 0; i < categories.Count; i++)
            {
                string category = categories[i];
                var color = colors[i % colors.Length];

                double[] values = new double[dayPositions.Length];
                for (int d = 0; d < dailyData.Count; d++)
                {
                    if (dailyData[d].ByCategory.TryGetValue(category, out double val))
                        values[d] = val;
                }

                // Добавляем столбец для этой категории
                var bar = plt.Add.Bars(dayPositions, values);
                bar.Label = category;
                bar.Color = color;

                // Устанавливаем нижнюю границу для стека
                for (int b = 0; b < bar.Bars.Count; b++)
                {
                    bar.Bars[b].ValueBase = bottoms[b];
                    bottoms[b] += values[b];
                }
            }

            // Настройка осей
            plt.Title($"Расходы за {startDate:MMMM yyyy}");
            plt.YLabel("Сумма (₽)");
            plt.XLabel("День месяца");

            // Подписи дней
            plt.Axes.Bottom.SetTicks(dayPositions, dailyData.Select(d => d.Day.ToString()).ToArray());
            plt.Axes.Bottom.TickLabelStyle.Rotation = 0;

            // Легенда
            plt.ShowLegend(Alignment.UpperRight);

            return plt.GetImageBytes(1200, 600, ImageFormat.Png);
        }

        public async Task<FinanceStatistics> GetFinanceStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            var records = await _context.FinancialRecords
                .Where(f => f.TransactionDate >= start && f.TransactionDate <= end)
                .ToListAsync();

            var incomeRecords = records.Where(r => r.Type == FinancialRecordType.Income).ToList();
            var expenseRecords = records.Where(r => r.Type == FinancialRecordType.Expense).ToList();

            // Разделяем по статусам
            var workingIncome = incomeRecords.Where(r => r.FundStatus == IFinanceService.FundStatus.Working.ToString()).Sum(r => r.Amount);
            var workingExpense = expenseRecords.Where(r => r.FundStatus == IFinanceService.FundStatus.Working.ToString()).Sum(r => r.Amount);
            var reservedIncome = incomeRecords.Where(r => r.FundStatus == IFinanceService.FundStatus.Reserved.ToString()).Sum(r => r.Amount);
            var reservedExpense = expenseRecords.Where(r => r.FundStatus == IFinanceService.FundStatus.Reserved.ToString()).Sum(r => r.Amount);
            var blockedIncome = incomeRecords.Where(r => r.FundStatus == IFinanceService.FundStatus.Blocked.ToString()).Sum(r => r.Amount);
            var blockedExpense = expenseRecords.Where(r => r.FundStatus == IFinanceService.FundStatus.Blocked.ToString()).Sum(r => r.Amount);
            var transitIncome = incomeRecords.Where(r => r.FundStatus == IFinanceService.FundStatus.InTransit.ToString()).Sum(r => r.Amount);
            var transitExpense = expenseRecords.Where(r => r.FundStatus == IFinanceService.FundStatus.InTransit.ToString()).Sum(r => r.Amount);

            // Баланс по статусам
            var balanceByStatus = new Dictionary<string, decimal>
            {
                ["Working"] = workingIncome - workingExpense,
                ["Reserved"] = reservedIncome - reservedExpense,
                ["Blocked"] = blockedIncome - blockedExpense,
                ["InTransit"] = transitIncome - transitExpense
            };

            var statistics = new FinanceStatistics
            {
                TotalIncome = incomeRecords.Sum(r => r.Amount),
                TotalExpenses = expenseRecords.Sum(r => r.Amount),
                Balance = incomeRecords.Sum(r => r.Amount) - expenseRecords.Sum(r => r.Amount),
                WorkingCapital = workingIncome - workingExpense,
                ReservedFunds = reservedIncome - reservedExpense,
                BalanceByStatus = balanceByStatus,

                IncomeByCategory = incomeRecords
                    .GroupBy(r => r.Category)
                    .Select(g => new CategoryStat
                    {
                        Category = g.Key,
                        Total = g.Sum(r => r.Amount),
                        Count = g.Count()
                    })
                    .OrderByDescending(c => c.Total)
                    .ToList(),

                ExpensesByCategory = expenseRecords
                    .GroupBy(r => r.Category)
                    .Select(g => new CategoryStat
                    {
                        Category = g.Key,
                        Total = g.Sum(r => r.Amount),
                        Count = g.Count()
                    })
                    .OrderByDescending(c => c.Total)
                    .ToList(),

                MonthlyIncome = incomeRecords.Where(r => r.TransactionDate.Month == DateTime.UtcNow.Month).Sum(r => r.Amount),
                MonthlyExpenses = expenseRecords.Where(r => r.TransactionDate.Month == DateTime.UtcNow.Month).Sum(r => r.Amount)
            };

            // Рассчитываем тренд по месяцам
            var monthlyTrend = new Dictionary<string, decimal>();
            for (int i = 5; i >= 0; i--)
            {
                var month = DateTime.UtcNow.AddMonths(-i);
                var monthKey = month.ToString("yyyy-MM");
                var monthIncome = incomeRecords
                    .Where(r => r.TransactionDate.Month == month.Month && r.TransactionDate.Year == month.Year)
                    .Sum(r => r.Amount);
                monthlyTrend[monthKey] = monthIncome;
            }
            statistics.MonthlyTrend = monthlyTrend;

            return statistics;
        }

        public async Task<CommissionStatistics> GetCommissionStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = endDate ?? DateTime.UtcNow;

            var commissions = await _context.FinancialRecords
                .Where(f => f.Category == "Комиссии" &&
                           f.TransactionDate >= start &&
                           f.TransactionDate <= end)
                .ToListAsync();

            // Группировка по типу комиссии (из описания)
            var commissionsByType = new Dictionary<string, decimal>();
            foreach (var commission in commissions)
            {
                var type = "other";
                if (commission.Description.Contains("transfer")) type = "transfer";
                else if (commission.Description.Contains("withdrawal")) type = "withdrawal";
                else if (commission.Description.Contains("exchange")) type = "exchange";

                if (!commissionsByType.ContainsKey(type))
                    commissionsByType[type] = 0;
                commissionsByType[type] += commission.Amount;
            }

            return new CommissionStatistics
            {
                TotalCommissions = commissions.Sum(c => c.Amount),
                CommissionCount = commissions.Count,
                AverageCommission = commissions.Any() ? commissions.Average(c => c.Amount) : 0,
                CommissionsByProject = commissions
                    .Where(c => c.ProjectId.HasValue)
                    .GroupBy(c => c.ProjectId.Value)
                    .ToDictionary(g => $"Project_{g.Key}", g => g.Sum(c => c.Amount)),
                LargestCommission = commissions.Any() ? commissions.Max(c => c.Amount) : 0,
                CommissionsByType = commissionsByType
            };
        }

        // ========== МЕТОДЫ ДЛЯ КАТЕГОРИЙ ==========

        public async Task<List<CategoryStat>> GetIncomeByCategoryAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.FinancialRecords.Where(f => f.Type == FinancialRecordType.Income);

            if (startDate.HasValue)
                query = query.Where(f => f.TransactionDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(f => f.TransactionDate <= endDate.Value);

            var result = await query
                .GroupBy(f => f.Category)
                .Select(g => new CategoryStat
                {
                    Category = g.Key,
                    Total = g.Sum(f => f.Amount),
                    Count = g.Count()
                })
                .ToListAsync();

            // Сортируем в памяти
            return result.OrderByDescending(c => c.Total).ToList();
        }

        public async Task<List<CategoryStat>> GetExpensesByCategoryAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.FinancialRecords.Where(f => f.Type == FinancialRecordType.Expense);

            if (startDate.HasValue)
                query = query.Where(f => f.TransactionDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(f => f.TransactionDate <= endDate.Value);

            var result = await query
                .GroupBy(f => f.Category)
                .Select(g => new CategoryStat
                {
                    Category = g.Key,
                    Total = g.Sum(f => f.Amount),
                    Count = g.Count()
                })
                .ToListAsync();

            // Сортируем в памяти, а не в SQL
            return result.OrderByDescending(c => c.Total).ToList();
        }

        public async Task<List<string>> GetAllCategoriesAsync(FinancialRecordType? type = null)
        {
            var query = _context.FinancialRecords.AsQueryable();

            if (type.HasValue)
                query = query.Where(f => f.Type == type.Value);

            return await query
                .Select(f => f.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        // ========== МЕТОДЫ ДЛЯ ТРЕНДОВ ==========

        public async Task<Dictionary<string, decimal>> GetMonthlyTrendAsync(FinancialRecordType type, int months = 6)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddMonths(-months);

            var records = await _context.FinancialRecords
                .Where(f => f.Type == type && f.TransactionDate >= startDate && f.TransactionDate <= endDate)
                .ToListAsync();

            var monthlyData = new Dictionary<string, decimal>();

            for (int i = 0; i < months; i++)
            {
                var month = startDate.AddMonths(i);
                var monthKey = month.ToString("yyyy-MM");
                var monthAmount = records
                    .Where(r => r.TransactionDate.Month == month.Month && r.TransactionDate.Year == month.Year)
                    .Sum(r => r.Amount);

                monthlyData[monthKey] = monthAmount;
            }

            return monthlyData;
        }

        public async Task<Dictionary<string, decimal>> GetDailyTrendAsync(FinancialRecordType type, int days = 30)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-days);

            var records = await _context.FinancialRecords
                .Where(f => f.Type == type && f.TransactionDate >= startDate && f.TransactionDate <= endDate)
                .ToListAsync();

            var dailyData = new Dictionary<string, decimal>();

            for (int i = 0; i < days; i++)
            {
                var day = startDate.AddDays(i);
                var dayKey = day.ToString("yyyy-MM-dd");
                var dayAmount = records
                    .Where(r => r.TransactionDate.Date == day.Date)
                    .Sum(r => r.Amount);

                dailyData[dayKey] = dayAmount;
            }

            return dailyData;
        }

        // ========== МЕТОДЫ ПРОВЕРКИ ==========

        public async Task<bool> RecordExistsAsync(int recordId)
        {
            return await _context.FinancialRecords.AnyAsync(f => f.Id == recordId);
        }

        public async Task<int> GetRecordsCountAsync()
        {
            return await _context.FinancialRecords.CountAsync();
        }

        public async Task<decimal> GetAverageTransactionAmountAsync(FinancialRecordType? type = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.FinancialRecords.AsQueryable();

            if (type.HasValue)
                query = query.Where(f => f.Type == type.Value);

            if (startDate.HasValue)
                query = query.Where(f => f.TransactionDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(f => f.TransactionDate <= endDate.Value);

            if (!await query.AnyAsync())
                return 0;

            return await query.AverageAsync(f => f.Amount);
        }

        // ========== МЕТОДЫ ЭКСПОРТА ==========

        public async Task<byte[]> ExportToCsvAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var records = await GetRecordsByDateRangeAsync(startDate ?? DateTime.UtcNow.AddMonths(-1), endDate ?? DateTime.UtcNow);

            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream);

            // Заголовки
            await writer.WriteLineAsync("ID,Date,Type,Category,Description,Amount,Currency,Status,Project,User,Contact");

            // Данные
            foreach (var record in records)
            {
                var line = $"{record.Id}," +
                          $"{record.TransactionDate:yyyy-MM-dd}," +
                          $"{record.Type}," +
                          $"{record.Category}," +
                          $"{EscapeCsv(record.Description)}," +
                          $"{record.Amount}," +
                          $"{record.Currency}," +
                          $"{record.FundStatus}," +
                          $"{record.Project?.Name ?? "N/A"}," +
                          $"{record.User?.Username ?? "N/A"}," +
                          $"{record.Contact?.TelegramUsername ?? "N/A"}";

                await writer.WriteLineAsync(line);
            }

            await writer.FlushAsync();
            return memoryStream.ToArray();
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";

            return value;
        }
    }
}