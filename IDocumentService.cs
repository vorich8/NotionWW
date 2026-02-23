using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public interface IDocumentService
    {
        // CRUD операции
        Task<DbDocument> CreateDocumentAsync(DbDocument document);
        Task<DbDocument?> GetDocumentAsync(int documentId);
        Task<List<DbDocument>> GetAllDocumentsAsync();
        Task<List<DbDocument>> GetDocumentsByProjectAsync(string projectName);
        Task<List<DbDocument>> GetDocumentsByTypeAsync(string documentType);
        Task<bool> UpdateDocumentAsync(DbDocument document);
        Task<bool> DeleteDocumentAsync(int documentId);

        // Поиск
        Task<List<DbDocument>> SearchDocumentsAsync(string searchTerm);

        // Статистика
        Task<DocumentStatistics> GetDocumentStatisticsAsync();
    }

    public class DocumentStatistics
    {
        public int TotalDocuments { get; set; }
        public Dictionary<string, int> DocumentsByProject { get; set; } = new();
        public Dictionary<string, int> DocumentsByType { get; set; } = new();
    }
}