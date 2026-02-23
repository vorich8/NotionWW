// TaskService.cs - ПОЛНОСТЬЮ ИСПРАВЛЕННЫЙ
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskService> _logger;

        public TaskService(ApplicationDbContext context, ILogger<TaskService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TeamTask?> CreateTaskAsync(string title, string? description, int projectId,
            long assignedToUserId, long createdByUserId, DateTime? dueDate = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Проверяем существование проекта
                var project = await _context.Projects.FindAsync(projectId);
                if (project == null)
                {
                    _logger.LogWarning("Project {ProjectId} not found when creating task", projectId);
                    await transaction.RollbackAsync();
                    return null;
                }

                // Проверяем существование пользователя, которому назначается задача
                var assignedUser = await _context.Users.FindAsync(assignedToUserId);
                if (assignedUser == null)
                {
                    _logger.LogWarning("Assigned user {UserId} not found when creating task", assignedToUserId);
                    await transaction.RollbackAsync();
                    return null;
                }

                // Проверяем существование пользователя, который создает задачу
                var createdByUser = await _context.Users.FindAsync(createdByUserId);
                if (createdByUser == null)
                {
                    _logger.LogWarning("Created by user {UserId} not found when creating task", createdByUserId);
                    await transaction.RollbackAsync();
                    return null;
                }

                var task = new TeamTask
                {
                    Title = title.Trim(),
                    Description = description?.Trim(),
                    ProjectId = projectId,
                    AssignedToUserId = assignedToUserId,
                    CreatedByUserId = createdByUserId,
                    Status = TeamTaskStatus.Active, // ← ИЗМЕНИЛОСЬ
                    DueDate = dueDate?.ToUniversalTime(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Created task: {Title} (ID: {TaskId}) assigned to user {AssignedUserId}",
                    title, task.Id, assignedToUserId);
                return task;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating task: {Title}", title);
                return null;
            }
        }

        public async Task<List<TeamTask>> GetAllTasksAsync()
        {
            return await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .OrderByDescending(t => t.CreatedAt)
                .Take(100)
                .ToListAsync();
        }

        public async Task<List<TeamTask>> GetActiveTasksAsync()
        {
            return await _context.Tasks
                .Where(t => t.Status == TeamTaskStatus.Active) // ← ИЗМЕНИЛОСЬ
                .Include(t => t.Project)
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                .ThenByDescending(t => t.CreatedAt)
                .Take(50)
                .ToListAsync();
        }

        public async Task<List<TeamTask>> GetCompletedTasksAsync()
        {
            return await _context.Tasks
                .Where(t => t.Status == TeamTaskStatus.Completed) // ← ИЗМЕНИЛОСЬ
                .Include(t => t.Project)
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .OrderByDescending(t => t.CompletedAt ?? t.UpdatedAt ?? t.CreatedAt)
                .Take(50)
                .ToListAsync();
        }

        public async Task<List<TeamTask>> GetTasksByUserAsync(long userId)
        {
            return await _context.Tasks
                .Where(t => t.AssignedToUserId == userId)
                .Include(t => t.Project)
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .OrderByDescending(t => t.CreatedAt)
                .Take(50)
                .ToListAsync();
        }

        public async Task<List<TeamTask>> GetTasksByProjectAsync(int projectId)
        {
            return await _context.Tasks
                .Where(t => t.ProjectId == projectId)
                .Include(t => t.Project)
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .OrderBy(t => t.Status)
                .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
                .Take(50)
                .ToListAsync();
        }

        public async Task<TeamTask?> GetTaskAsync(int taskId)
        {
            return await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .FirstOrDefaultAsync(t => t.Id == taskId);
        }

        public async Task<bool> UpdateTaskAsync(TeamTask task)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                task.UpdatedAt = DateTime.UtcNow;

                var entry = _context.Attach(task);
                entry.State = EntityState.Modified;

                // Исключаем поля, которые не должны обновляться
                entry.Property(x => x.CreatedAt).IsModified = false;
                entry.Property(x => x.CreatedByUserId).IsModified = false;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Updated task {TaskId} ({Title})", task.Id, task.Title);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating task {TaskId}", task.Id);
                return false;
            }
        }

        public async Task<bool> DeleteTaskAsync(int taskId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var task = await _context.Tasks.FindAsync(taskId);
                if (task == null)
                {
                    _logger.LogWarning("Task {TaskId} not found for deletion", taskId);
                    await transaction.RollbackAsync();
                    return false;
                }

                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Deleted task {TaskId} ({Title})", taskId, task.Title);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting task {TaskId}", taskId);
                return false;
            }
        }

        public async Task<bool> CompleteTaskAsync(int taskId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var task = await _context.Tasks.FindAsync(taskId);
                if (task == null)
                {
                    _logger.LogWarning("Task {TaskId} not found for completion", taskId);
                    await transaction.RollbackAsync();
                    return false;
                }

                task.Status = TeamTaskStatus.Completed; // ← ИЗМЕНИЛОСЬ
                task.CompletedAt = DateTime.UtcNow;
                task.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Completed task {TaskId} ({Title})", taskId, task.Title);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error completing task {TaskId}", taskId);
                return false;
            }
        }

        public async Task<bool> ArchiveTaskAsync(int taskId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var task = await _context.Tasks.FindAsync(taskId);
                if (task == null)
                {
                    _logger.LogWarning("Task {TaskId} not found for archiving", taskId);
                    await transaction.RollbackAsync();
                    return false;
                }

                task.Status = TeamTaskStatus.Archived; // ← ИЗМЕНИЛОСЬ
                task.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Archived task {TaskId} ({Title})", taskId, task.Title);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error archiving task {TaskId}", taskId);
                return false;
            }
        }

        public async Task<bool> ActivateTaskAsync(int taskId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var task = await _context.Tasks.FindAsync(taskId);
                if (task == null)
                {
                    _logger.LogWarning("Task {TaskId} not found for activation", taskId);
                    await transaction.RollbackAsync();
                    return false;
                }

                task.Status = TeamTaskStatus.Active; // ← ИЗМЕНИЛОСЬ
                task.CompletedAt = null;
                task.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Activated task {TaskId} ({Title})", taskId, task.Title);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error activating task {TaskId}", taskId);
                return false;
            }
        }

        public async Task<bool> UpdateTaskStatusAsync(int taskId, TeamTaskStatus newStatus) // ← ИЗМЕНИЛОСЬ
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var task = await _context.Tasks.FindAsync(taskId);
                if (task == null)
                {
                    _logger.LogWarning("Task {TaskId} not found for status update", taskId);
                    await transaction.RollbackAsync();
                    return false;
                }

                var oldStatus = task.Status;

                if (oldStatus == newStatus)
                {
                    _logger.LogInformation("Task {TaskId} already has status {Status}", taskId, newStatus);
                    await transaction.RollbackAsync();
                    return true;
                }

                task.Status = newStatus;
                task.UpdatedAt = DateTime.UtcNow;

                if (newStatus == TeamTaskStatus.Completed) // ← ИЗМЕНИЛОСЬ
                    task.CompletedAt = DateTime.UtcNow;
                else if (oldStatus == TeamTaskStatus.Completed && newStatus != TeamTaskStatus.Completed) // ← ИЗМЕНИЛОСЬ
                    task.CompletedAt = null;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Updated task {TaskId} status from {OldStatus} to {NewStatus}",
                    taskId, oldStatus, newStatus);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating task status for {TaskId}", taskId);
                return false;
            }
        }

        public async Task<bool> ReassignTaskAsync(int taskId, long newUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var task = await _context.Tasks.FindAsync(taskId);
                if (task == null)
                {
                    _logger.LogWarning("Task {TaskId} not found for reassignment", taskId);
                    await transaction.RollbackAsync();
                    return false;
                }

                var newUser = await _context.Users.FindAsync(newUserId);
                if (newUser == null)
                {
                    _logger.LogWarning("New user {UserId} not found for task reassignment", newUserId);
                    await transaction.RollbackAsync();
                    return false;
                }

                var oldUserId = task.AssignedToUserId;
                task.AssignedToUserId = newUserId;
                task.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Reassigned task {TaskId} from user {OldUserId} to user {NewUserId}",
                    taskId, oldUserId, newUserId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error reassigning task {TaskId}", taskId);
                return false;
            }
        }

        public async Task<bool> MoveTaskToProjectAsync(int taskId, int newProjectId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var task = await _context.Tasks.FindAsync(taskId);
                if (task == null)
                {
                    _logger.LogWarning("Task {TaskId} not found for moving", taskId);
                    await transaction.RollbackAsync();
                    return false;
                }

                var newProject = await _context.Projects.FindAsync(newProjectId);
                if (newProject == null)
                {
                    _logger.LogWarning("New project {ProjectId} not found for task move", newProjectId);
                    await transaction.RollbackAsync();
                    return false;
                }

                var oldProjectId = task.ProjectId;
                task.ProjectId = newProjectId;
                task.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Moved task {TaskId} from project {OldProjectId} to project {NewProjectId}",
                    taskId, oldProjectId, newProjectId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error moving task {TaskId}", taskId);
                return false;
            }
        }

        public async Task<List<TeamTask>> SearchTasksAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<TeamTask>();

            var searchTermLower = searchTerm.ToLowerInvariant();

            return await _context.Tasks
                .Where(t =>
                    t.Title.ToLower().Contains(searchTermLower) ||
                    (t.Description != null && t.Description.ToLower().Contains(searchTermLower)))
                .Include(t => t.Project)
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .OrderByDescending(t => t.CreatedAt)
                .Take(50)
                .ToListAsync();
        }

        public async Task<List<TeamTask>> GetOverdueTasksAsync()
        {
            var today = DateTime.UtcNow.Date;

            return await _context.Tasks
                .Where(t => t.Status == TeamTaskStatus.Active && // ← ИЗМЕНИЛОСЬ
                           t.DueDate.HasValue &&
                           t.DueDate.Value.Date < today)
                .Include(t => t.Project)
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .OrderBy(t => t.DueDate)
                .Take(50)
                .ToListAsync();
        }

        public async Task<List<TeamTask>> GetTasksDueSoonAsync(int days = 3)
        {
            var today = DateTime.UtcNow.Date;
            var dueDate = today.AddDays(days);

            return await _context.Tasks
                .Where(t => t.Status == TeamTaskStatus.Active && // ← ИЗМЕНИЛОСЬ
                           t.DueDate.HasValue &&
                           t.DueDate.Value.Date >= today &&
                           t.DueDate.Value.Date <= dueDate)
                .Include(t => t.Project)
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .OrderBy(t => t.DueDate)
                .Take(50)
                .ToListAsync();
        }

        public async Task<PaginatedResult<TeamTask>> GetTasksPaginatedAsync(int page = 1, int pageSize = 10)
        {
            var total = await _context.Tasks.CountAsync();
            var tasks = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedResult<TeamTask>(tasks, page, pageSize, total);
        }

        public async Task<PaginatedResult<TeamTask>> GetTasksByUserPaginatedAsync(long userId, int page = 1, int pageSize = 10)
        {
            var query = _context.Tasks.Where(t => t.AssignedToUserId == userId);
            var total = await query.CountAsync();
            var tasks = await query
                .Include(t => t.Project)
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedResult<TeamTask>(tasks, page, pageSize, total);
        }

        public async Task<int> GetTasksCountAsync()
        {
            return await _context.Tasks.CountAsync();
        }

        public async Task<int> GetActiveTasksCountAsync()
        {
            return await _context.Tasks
                .Where(t => t.Status == TeamTaskStatus.Active) // ← ИЗМЕНИЛОСЬ
                .CountAsync();
        }

        public async Task<int> GetCompletedTasksCountAsync()
        {
            return await _context.Tasks
                .Where(t => t.Status == TeamTaskStatus.Completed) // ← ИЗМЕНИЛОСЬ
                .CountAsync();
        }

        public async Task<Dictionary<TeamTaskStatus, int>> GetTasksStatisticsAsync() // ← ИЗМЕНИЛОСЬ
        {
            var stats = await _context.Tasks
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = new Dictionary<TeamTaskStatus, int>();

            foreach (var stat in stats)
            {
                result[stat.Status] = stat.Count;
            }

            // Добавляем отсутствующие статусы с нулевым значением
            foreach (TeamTaskStatus status in Enum.GetValues(typeof(TeamTaskStatus)))
            {
                if (!result.ContainsKey(status))
                {
                    result[status] = 0;
                }
            }

            return result;
        }

        public async Task<Dictionary<long, int>> GetUserTasksStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Tasks.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(t => t.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.CreatedAt <= endDate.Value);

            var stats = await query
                .GroupBy(t => t.AssignedToUserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync();

            return stats.ToDictionary(s => s.UserId, s => s.Count);
        }

        public async Task<bool> TaskExistsAsync(int taskId)
        {
            return await _context.Tasks.AnyAsync(t => t.Id == taskId);
        }

        public async Task<bool> CanUserAccessTaskAsync(int taskId, long userId)
        {
            var task = await _context.Tasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return false;

            // Пользователь может получить доступ к задаче если:
            // 1. Он назначен на задачу
            // 2. Он создал задачу
            // 3. Он создатель проекта
            // 4. Он администратор (проверяется на уровне выше)
            return task.AssignedToUserId == userId ||
                   task.CreatedByUserId == userId ||
                   (task.Project != null && task.Project.CreatedByUserId == userId);
        }

        public async Task<List<TeamTask>> GetTasksByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Tasks
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .Include(t => t.Project)
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .ToListAsync();
        }
    }
}