using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public interface IPlanService
    {
        // CRUD операции
        Task<Plan> CreatePlanAsync(string title, string content, long userId);
        Task<Plan?> GetPlanAsync(int planId);
        Task<List<Plan>> GetAllPlansAsync();
        Task<List<Plan>> SearchPlansAsync(string searchTerm);
        Task<bool> UpdatePlanAsync(Plan plan);
        Task<bool> DeletePlanAsync(int planId);

        // Экспорт
        Task<byte[]> ExportPlanToTxtAsync(int planId);
    }
}