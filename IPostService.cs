using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public interface IPostService
    {
        // CRUD операции
        Task<DbPost> CreatePostAsync(DbPost post);
        Task<DbPost?> GetPostAsync(int postId);
        Task<List<DbPost>> GetAllPostsAsync();
        Task<List<DbPost>> GetPostsByChannelAsync(string channel);
        Task<List<DbPost>> GetPostsByStatusAsync(string status);
        Task<bool> UpdatePostAsync(DbPost post);
        Task<bool> DeletePostAsync(int postId);

        // Поиск
        Task<List<DbPost>> SearchPostsAsync(string searchTerm);

        // Статистика
        Task<PostStatistics> GetPostStatisticsAsync();
    }

    public class PostStatistics
    {
        public int TotalPosts { get; set; }
        public int PublishedPosts { get; set; }
        public int DraftPosts { get; set; }
        public int ScheduledPosts { get; set; }
        public Dictionary<string, int> PostsByChannel { get; set; } = new();
    }
}