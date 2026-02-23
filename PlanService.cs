using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public class PlanService : IPlanService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PlanService> _logger;

        public PlanService(ApplicationDbContext context, ILogger<PlanService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Plan> CreatePlanAsync(string title, string content, long userId)
        {
            try
            {
                var plan = new Plan
                {
                    Title = title,
                    Content = content,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Plans.Add(plan);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created plan: {Title} by user {UserId}", title, userId);
                return plan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating plan");
                throw;
            }
        }

        public async Task<Plan?> GetPlanAsync(int planId)
        {
            return await _context.Plans
                .Include(p => p.CreatedBy)
                .FirstOrDefaultAsync(p => p.Id == planId);
        }

        public async Task<List<Plan>> GetAllPlansAsync()
        {
            return await _context.Plans
                .Include(p => p.CreatedBy)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Plan>> SearchPlansAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Plan>();

            searchTerm = searchTerm.ToLower();
            return await _context.Plans
                .Where(p => p.Title.ToLower().Contains(searchTerm) ||
                            p.Content.ToLower().Contains(searchTerm))
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .ToListAsync();
        }

        public async Task<bool> UpdatePlanAsync(Plan plan)
        {
            try
            {
                var existing = await _context.Plans.FindAsync(plan.Id);
                if (existing == null)
                    return false;

                existing.Title = plan.Title;
                existing.Content = plan.Content;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated plan: {Title}", plan.Title);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating plan {PlanId}", plan.Id);
                return false;
            }
        }

        public async Task<bool> DeletePlanAsync(int planId)
        {
            try
            {
                var plan = await _context.Plans.FindAsync(planId);
                if (plan == null)
                    return false;

                _context.Plans.Remove(plan);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted plan: {Title}", plan.Title);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting plan {PlanId}", planId);
                return false;
            }
        }

        public async Task<byte[]> ExportPlanToTxtAsync(int planId)
        {
            var plan = await _context.Plans.FindAsync(planId);
            if (plan == null)
                return Array.Empty<byte>();

            var content = $"ПЛАН: {plan.Title}\n" +
                         $"Создан: {plan.CreatedAt:dd.MM.yyyy HH:mm}\n" +
                         $"Автор: {plan.CreatedBy?.Username ?? "Неизвестно"}\n" +
                         $"Последнее изменение: {plan.UpdatedAt?.ToString("dd.MM.yyyy HH:mm") ?? "—"}\n" +
                         $"{new string('=', 50)}\n\n" +
                         $"{plan.Content}";

            return Encoding.UTF8.GetBytes(content);
        }
    }
}