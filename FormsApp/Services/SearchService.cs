using FormsApp.Data;
using FormsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FormsApp.Services
{
    public class SearchService : ISearchService
    {
        private readonly ApplicationDbContext _context;
        
        public SearchService(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<IEnumerable<FormTemplate>> SearchTemplatesAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await _context.FormTemplates.Take(10).ToListAsync();
                
            // Get normalized search term for case-insensitive comparison
            var normalizedSearchTerm = searchTerm.ToLower();
                
            // Search across multiple entities and properties
            return await _context.FormTemplates
                .Include(t => t.Creator)
                .Include(t => t.TopicNavigation)
                .Include(t => t.TemplateTags)
                    .ThenInclude(tt => tt.Tag)
                .Where(t => t.Title.ToLower().Contains(normalizedSearchTerm) || 
                            t.Description.ToLower().Contains(normalizedSearchTerm) || 
                            t.Creator.UserName.ToLower().Contains(normalizedSearchTerm) ||
                            t.Creator.Email.ToLower().Contains(normalizedSearchTerm) ||
                            t.TopicNavigation.Name.ToLower().Contains(normalizedSearchTerm) ||
                            t.TemplateTags.Any(tt => tt.Tag.Name.ToLower().Contains(normalizedSearchTerm)) ||
                            t.Questions.Any(q => q.Text.ToLower().Contains(normalizedSearchTerm) || 
                                               q.Description.ToLower().Contains(normalizedSearchTerm)))
                .Where(t => t.IsPublic) // Only return public templates
                .ToListAsync();
        }
        
        public async Task<IEnumerable<Tag>> GetTagsStartingWithAsync(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return await _context.Tags.Take(10).ToListAsync();
                
            return await _context.Tags
                .Where(t => t.Name.StartsWith(prefix))
                .OrderByDescending(t => t.UsageCount)
                .Take(10)
                .ToListAsync();
        }
    }
} 