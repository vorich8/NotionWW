using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public class ProjectService : IProjectService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProjectService> _logger;

        public ProjectService(ApplicationDbContext context, ILogger<ProjectService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Project?> CreateProjectAsync(string name, string? description, string? link, long createdByUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Проверяем существование пользователя
                var user = await _context.Users.FindAsync(createdByUserId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found when creating project", createdByUserId);
                    await transaction.RollbackAsync();
                    return null;
                }

                // Проверяем уникальность названия (регистронезависимо)
                var existingProject = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Name.ToLower() == name.ToLower());

                if (existingProject != null)
                {
                    _logger.LogWarning("Project with name {Name} already exists (ID: {ProjectId})",
                        name, existingProject.Id);
                    await transaction.RollbackAsync();
                    return null;
                }

                var project = new Project
                {
                    Name = name.Trim(),
                    Description = description?.Trim(),
                    Link = link?.Trim(),
                    Status = ProjectStatus.Pending,
                    CreatedByUserId = createdByUserId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                // Создаем начальный статус в истории
                var statusUpdate = new StatusUpdate
                {
                    ProjectId = project.Id,
                    Text = $"Проект создан: {name}",
                    CreatedByUserId = createdByUserId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.StatusUpdates.Add(statusUpdate);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Created project: {Name} (ID: {ProjectId}) by user {UserId}",
                    name, project.Id, createdByUserId);
                return project;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating project: {Name}", name);
                return null;
            }
        }

        public async Task<List<Project>> GetAllProjectsAsync()
        {
            return await _context.Projects
                .Include(p => p.CreatedBy)
                .Include(p => p.Tasks)
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .ToListAsync();
        }

        public async Task<PaginatedResult<Project>> GetProjectsPaginatedAsync(int page = 1, int pageSize = 10)
        {
            var total = await _context.Projects.CountAsync();
            var projects = await _context.Projects
                .Include(p => p.CreatedBy)
                .Include(p => p.Tasks)
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedResult<Project>(projects, page, pageSize, total);
        }

        public async Task<Project?> GetProjectAsync(int projectId)
        {
            return await _context.Projects
                .Include(p => p.CreatedBy)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.AssignedTo)
                .Include(p => p.StatusUpdates)
                    .ThenInclude(su => su.CreatedBy)
                .Include(p => p.Documentations)
                .FirstOrDefaultAsync(p => p.Id == projectId);
        }

        public async Task<Project?> GetProjectWithDetailsAsync(int projectId)
        {
            return await _context.Projects
                .Include(p => p.CreatedBy)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.AssignedTo)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.CreatedBy)
                .Include(p => p.StatusUpdates)
                    .ThenInclude(su => su.CreatedBy)
                .Include(p => p.Documentations)
                    .ThenInclude(d => d.CreatedBy)
                .FirstOrDefaultAsync(p => p.Id == projectId);
        }

        public async Task<bool> UpdateProjectAsync(Project project)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                project.UpdatedAt = DateTime.UtcNow;

                // Используем Attach для оптимизации
                var entry = _context.Attach(project);
                entry.State = EntityState.Modified;

                // Исключаем поля, которые не должны обновляться
                entry.Property(x => x.CreatedByUserId).IsModified = false;
                entry.Property(x => x.CreatedAt).IsModified = false;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Updated project {ProjectId} ({Name})",
                    project.Id, project.Name);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating project {ProjectId}", project.Id);
                return false;
            }
        }

        public async Task<bool> DeleteProjectAsync(int projectId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var project = await _context.Projects
                    .Include(p => p.Tasks)
                    .Include(p => p.StatusUpdates)
                    .Include(p => p.Documentations)
                    .FirstOrDefaultAsync(p => p.Id == projectId);

                if (project == null)
                {
                    _logger.LogWarning("Project {ProjectId} not found for deletion", projectId);
                    await transaction.RollbackAsync();
                    return false;
                }

                // Удаляем связанные сущности
                _context.Tasks.RemoveRange(project.Tasks);
                _context.StatusUpdates.RemoveRange(project.StatusUpdates);
                _context.ProjectDocumentations.RemoveRange(project.Documentations);

                // Удаляем проект
                _context.Projects.Remove(project);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Deleted project {ProjectId} ({Name}) with all related data",
                    projectId, project.Name);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting project {ProjectId}", projectId);
                return false;
            }
        }

        public async Task<bool> UpdateProjectStatusAsync(int projectId, ProjectStatus newStatus)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var project = await _context.Projects.FindAsync(projectId);
                if (project == null)
                {
                    _logger.LogWarning("Project {ProjectId} not found for status update", projectId);
                    await transaction.RollbackAsync();
                    return false;
                }

                var oldStatus = project.Status;

                if (oldStatus == newStatus)
                {
                    _logger.LogInformation("Project {ProjectId} already has status {Status}",
                        projectId, newStatus);
                    await transaction.RollbackAsync();
                    return true; // Не ошибка, просто статус уже такой же
                }

                project.Status = newStatus;
                project.UpdatedAt = DateTime.UtcNow;

                // Создаем запись в истории статусов
                var statusUpdate = new StatusUpdate
                {
                    ProjectId = projectId,
                    Text = $"Статус изменен с {GetStatusName(oldStatus)} на {GetStatusName(newStatus)}",
                    CreatedByUserId = project.CreatedByUserId, // В реальности нужно передать ID текущего пользователя
                    CreatedAt = DateTime.UtcNow
                };

                _context.StatusUpdates.Add(statusUpdate);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Updated project {ProjectId} status from {OldStatus} to {NewStatus}",
                    projectId, oldStatus, newStatus);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating project status for {ProjectId}", projectId);
                return false;
            }
        }

        private string GetStatusName(ProjectStatus status)
        {
            return status switch
            {
                ProjectStatus.Pending => "🟡 Предстоит",
                ProjectStatus.InProgress => "🟠 В работе",
                ProjectStatus.Completed => "✅ Готово",
                _ => "❓ Неизвестно"
            };
        }

        public async Task<List<Project>> GetProjectsByStatusAsync(ProjectStatus status)
        {
            return await _context.Projects
                .Where(p => p.Status == status)
                .Include(p => p.CreatedBy)
                .Include(p => p.Tasks)
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Project>> GetProjectsByUserAsync(long userId)
        {
            return await _context.Projects
                .Where(p => p.CreatedByUserId == userId)
                .Include(p => p.CreatedBy)
                .Include(p => p.Tasks)
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Project>> SearchProjectsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Project>();

            var searchTermLower = searchTerm.ToLowerInvariant();

            return await _context.Projects
                .Where(p =>
                    p.Name.ToLower().Contains(searchTermLower) ||
                    (p.Description != null && p.Description.ToLower().Contains(searchTermLower)) ||
                    (p.Link != null && p.Link.ToLower().Contains(searchTermLower)))
                .Include(p => p.CreatedBy)
                .Include(p => p.Tasks)
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .Take(50)
                .ToListAsync();
        }

        public async Task<PaginatedResult<Project>> SearchProjectsPaginatedAsync(string searchTerm, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new PaginatedResult<Project>(new List<Project>(), page, pageSize, 0);

            var searchTermLower = searchTerm.ToLowerInvariant();

            var query = _context.Projects
                .Where(p =>
                    p.Name.ToLower().Contains(searchTermLower) ||
                    (p.Description != null && p.Description.ToLower().Contains(searchTermLower)) ||
                    (p.Link != null && p.Link.ToLower().Contains(searchTermLower)));

            var total = await query.CountAsync();
            var projects = await query
                .Include(p => p.CreatedBy)
                .Include(p => p.Tasks)
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedResult<Project>(projects, page, pageSize, total);
        }

        public async Task<int> GetProjectsCountAsync()
        {
            return await _context.Projects.CountAsync();
        }

        public async Task<int> GetProjectsCountByStatusAsync(ProjectStatus status)
        {
            return await _context.Projects
                .Where(p => p.Status == status)
                .CountAsync();
        }

        public async Task<Dictionary<ProjectStatus, int>> GetProjectsStatisticsAsync()
        {
            var stats = await _context.Projects
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = new Dictionary<ProjectStatus, int>();

            foreach (var stat in stats)
            {
                result[stat.Status] = stat.Count;
            }

            // Добавляем отсутствующие статусы с нулевым значением
            foreach (ProjectStatus status in Enum.GetValues(typeof(ProjectStatus)))
            {
                if (!result.ContainsKey(status))
                {
                    result[status] = 0;
                }
            }

            return result;
        }

        public async Task<bool> AddStatusUpdateAsync(int projectId, string text, long createdByUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var project = await _context.Projects.FindAsync(projectId);
                if (project == null)
                {
                    _logger.LogWarning("Project {ProjectId} not found for status update", projectId);
                    await transaction.RollbackAsync();
                    return false;
                }

                var statusUpdate = new StatusUpdate
                {
                    ProjectId = projectId,
                    Text = text.Trim(),
                    CreatedByUserId = createdByUserId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.StatusUpdates.Add(statusUpdate);
                project.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Added status update for project {ProjectId}", projectId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding status update for project {ProjectId}", projectId);
                return false;
            }
        }

        public async Task<List<StatusUpdate>> GetProjectStatusHistoryAsync(int projectId, int limit = 50)
        {
            return await _context.StatusUpdates
                .Where(su => su.ProjectId == projectId)
                .Include(su => su.CreatedBy)
                .OrderByDescending(su => su.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<bool> ProjectExistsAsync(int projectId)
        {
            return await _context.Projects.AnyAsync(p => p.Id == projectId);
        }

        public async Task<bool> ProjectNameExistsAsync(string name, int? excludeProjectId = null)
        {
            var query = _context.Projects
                .Where(p => p.Name.ToLower() == name.ToLower().Trim());

            if (excludeProjectId.HasValue)
            {
                query = query.Where(p => p.Id != excludeProjectId.Value);
            }

            return await query.AnyAsync();
        }
    }
}