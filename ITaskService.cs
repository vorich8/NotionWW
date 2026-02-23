// ITaskService.cs - ИСПРАВЛЕННЫЙ
using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public interface ITaskService
    {
        // Основные CRUD операции
        Task<TeamTask?> CreateTaskAsync(string title, string? description, int projectId,
            long assignedToUserId, long createdByUserId, DateTime? dueDate = null);

        Task<List<TeamTask>> GetAllTasksAsync();
        Task<List<TeamTask>> GetActiveTasksAsync();
        Task<List<TeamTask>> GetCompletedTasksAsync();
        Task<List<TeamTask>> GetTasksByUserAsync(long userId);
        Task<List<TeamTask>> GetTasksByProjectAsync(int projectId);

        Task<TeamTask?> GetTaskAsync(int taskId);
        Task<bool> UpdateTaskAsync(TeamTask task);
        Task<bool> DeleteTaskAsync(int taskId);

        // Операции со статусом
        Task<bool> CompleteTaskAsync(int taskId);
        Task<bool> ArchiveTaskAsync(int taskId);
        Task<bool> ActivateTaskAsync(int taskId);
        Task<bool> UpdateTaskStatusAsync(int taskId, TeamTaskStatus newStatus);

        // Перепривязка задач
        Task<bool> ReassignTaskAsync(int taskId, long newUserId);
        Task<bool> MoveTaskToProjectAsync(int taskId, int newProjectId);

        // Поиск и фильтрация
        Task<List<TeamTask>> SearchTasksAsync(string searchTerm);
        Task<List<TeamTask>> GetOverdueTasksAsync();
        Task<List<TeamTask>> GetTasksDueSoonAsync(int days = 3);

        // Пагинация
        Task<PaginatedResult<TeamTask>> GetTasksPaginatedAsync(int page = 1, int pageSize = 10);
        Task<PaginatedResult<TeamTask>> GetTasksByUserPaginatedAsync(long userId, int page = 1, int pageSize = 10);

        // Статистика
        Task<int> GetTasksCountAsync();
        Task<int> GetActiveTasksCountAsync();
        Task<int> GetCompletedTasksCountAsync();
        Task<Dictionary<TeamTaskStatus, int>> GetTasksStatisticsAsync();
        Task<Dictionary<long, int>> GetUserTasksStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

        // Валидация
        Task<bool> TaskExistsAsync(int taskId);
        Task<bool> CanUserAccessTaskAsync(int taskId, long userId);

        // Новый метод для получения задач по диапазону дат
        Task<List<TeamTask>> GetTasksByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}