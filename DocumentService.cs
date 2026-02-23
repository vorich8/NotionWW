using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamManagerBot.Models;

namespace TeamManagerBot.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(ApplicationDbContext context, ILogger<DocumentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DbDocument> CreateDocumentAsync(DbDocument document)
        {
            try
            {
                document.CreatedAt = DateTime.UtcNow;
                _context.DbDocuments.Add(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created document: {Title}", document.Title);
                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document");
                throw;
            }
        }

        public async Task<DbDocument?> GetDocumentAsync(int documentId)
        {
            return await _context.DbDocuments.FindAsync(documentId);
        }

        public async Task<List<DbDocument>> GetAllDocumentsAsync()
        {
            return await _context.DbDocuments
                .OrderBy(d => d.ProjectName)
                .ThenBy(d => d.Title)
                .ToListAsync();
        }

        public async Task<List<DbDocument>> GetDocumentsByProjectAsync(string projectName)
        {
            return await _context.DbDocuments
                .Where(d => d.ProjectName == projectName)
                .OrderBy(d => d.Title)
                .ToListAsync();
        }

        public async Task<List<DbDocument>> GetDocumentsByTypeAsync(string documentType)
        {
            return await _context.DbDocuments
                .Where(d => d.DocumentType == documentType)
                .OrderBy(d => d.ProjectName)
                .ToListAsync();
        }

        public async Task<bool> UpdateDocumentAsync(DbDocument document)
        {
            try
            {
                var existing = await _context.DbDocuments.FindAsync(document.Id);
                if (existing == null) return false;

                existing.Title = document.Title;
                existing.ProjectName = document.ProjectName;
                existing.Content = document.Content;
                existing.FilePath = document.FilePath;
                existing.DocumentType = document.DocumentType;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated document: {Title}", document.Title);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document {DocumentId}", document.Id);
                return false;
            }
        }

        public async Task<bool> DeleteDocumentAsync(int documentId)
        {
            try
            {
                var document = await _context.DbDocuments.FindAsync(documentId);
                if (document == null) return false;

                _context.DbDocuments.Remove(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted document: {Title}", document.Title);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
                return false;
            }
        }

        public async Task<List<DbDocument>> SearchDocumentsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<DbDocument>();

            searchTerm = searchTerm.ToLower();
            return await _context.DbDocuments
                .Where(d => d.Title.ToLower().Contains(searchTerm) ||
                            d.Content.ToLower().Contains(searchTerm) ||
                            (d.ProjectName != null && d.ProjectName.ToLower().Contains(searchTerm)))
                .OrderBy(d => d.ProjectName)
                .Take(20)
                .ToListAsync();
        }

        public async Task<DocumentStatistics> GetDocumentStatisticsAsync()
        {
            var documents = await _context.DbDocuments.ToListAsync();

            return new DocumentStatistics
            {
                TotalDocuments = documents.Count,
                DocumentsByProject = documents
                    .Where(d => !string.IsNullOrEmpty(d.ProjectName))
                    .GroupBy(d => d.ProjectName!)
                    .ToDictionary(g => g.Key, g => g.Count()),
                DocumentsByType = documents
                    .Where(d => !string.IsNullOrEmpty(d.DocumentType))
                    .GroupBy(d => d.DocumentType!)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }
    }
}