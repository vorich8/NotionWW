using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public interface IProjectService
    {
        // Основные CRUD операции
        Task<Project?> CreateProjectAsync(string name, string? description, string? link, long createdByUserId);
        Task<List<Project>> GetAllProjectsAsync();
        Task<Project?> GetProjectAsync(int projectId);
        Task<Project?> GetProjectWithDetailsAsync(int projectId);
        Task<bool> UpdateProjectAsync(Project project);
        Task<bool> DeleteProjectAsync(int projectId);
        Task<bool> UpdateProjectStatusAsync(int projectId, ProjectStatus newStatus);

        // Пагинация
        Task<PaginatedResult<Project>> GetProjectsPaginatedAsync(int page = 1, int pageSize = 10);
        Task<PaginatedResult<Project>> SearchProjectsPaginatedAsync(string searchTerm, int page = 1, int pageSize = 10);

        // Фильтрация и поиск
        Task<List<Project>> GetProjectsByStatusAsync(ProjectStatus status);
        Task<List<Project>> GetProjectsByUserAsync(long userId);
        Task<List<Project>> SearchProjectsAsync(string searchTerm);

        // Статистика
        Task<int> GetProjectsCountAsync();
        Task<int> GetProjectsCountByStatusAsync(ProjectStatus status);
        Task<Dictionary<ProjectStatus, int>> GetProjectsStatisticsAsync();

        // История статусов
        Task<bool> AddStatusUpdateAsync(int projectId, string text, long createdByUserId);
        Task<List<StatusUpdate>> GetProjectStatusHistoryAsync(int projectId, int limit = 50);

        // Валидация
        Task<bool> ProjectExistsAsync(int projectId);
        Task<bool> ProjectNameExistsAsync(string name, int? excludeProjectId = null);
    }
}