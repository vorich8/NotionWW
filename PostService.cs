using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public class PostService : IPostService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PostService> _logger;

        public PostService(ApplicationDbContext context, ILogger<PostService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DbPost> CreatePostAsync(DbPost post)
        {
            try
            {
                post.CreatedAt = DateTime.UtcNow;
                _context.DbPosts.Add(post);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created post: {Title}", post.Title);
                return post;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post");
                throw;
            }
        }

        public async Task<DbPost?> GetPostAsync(int postId)
        {
            return await _context.DbPosts.FindAsync(postId);
        }

        public async Task<List<DbPost>> GetAllPostsAsync()
        {
            return await _context.DbPosts
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<DbPost>> GetPostsByChannelAsync(string channel)
        {
            return await _context.DbPosts
                .Where(p => p.Channel == channel)
                .OrderByDescending(p => p.PublishDate ?? p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<DbPost>> GetPostsByStatusAsync(string status)
        {
            return await _context.DbPosts
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> UpdatePostAsync(DbPost post)
        {
            try
            {
                var existing = await _context.DbPosts.FindAsync(post.Id);
                if (existing == null) return false;

                existing.Title = post.Title;
                existing.Content = post.Content;
                existing.Channel = post.Channel;
                existing.PublishDate = post.PublishDate;
                existing.Link = post.Link;
                existing.Status = post.Status;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated post: {Title}", post.Title);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating post {PostId}", post.Id);
                return false;
            }
        }

        public async Task<bool> DeletePostAsync(int postId)
        {
            try
            {
                var post = await _context.DbPosts.FindAsync(postId);
                if (post == null) return false;

                _context.DbPosts.Remove(post);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted post: {Title}", post.Title);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post {PostId}", postId);
                return false;
            }
        }

        public async Task<List<DbPost>> SearchPostsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<DbPost>();

            searchTerm = searchTerm.ToLower();
            return await _context.DbPosts
                .Where(p => p.Title.ToLower().Contains(searchTerm) ||
                            p.Content.ToLower().Contains(searchTerm))
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .ToListAsync();
        }

        public async Task<PostStatistics> GetPostStatisticsAsync()
        {
            var posts = await _context.DbPosts.ToListAsync();

            return new PostStatistics
            {
                TotalPosts = posts.Count,
                PublishedPosts = posts.Count(p => p.Status == "Опубликовано"),
                DraftPosts = posts.Count(p => p.Status == "Черновик"),
                ScheduledPosts = posts.Count(p => p.Status == "Запланировано"),
                PostsByChannel = posts
                    .Where(p => !string.IsNullOrEmpty(p.Channel))
                    .GroupBy(p => p.Channel!)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }
    }
}