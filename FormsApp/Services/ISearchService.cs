using FormsApp.Models;

namespace FormsApp.Services
{
    public interface ISearchService
    {
        Task<IEnumerable<FormTemplate>> SearchTemplatesAsync(string searchTerm);
        Task<IEnumerable<Tag>> GetTagsStartingWithAsync(string prefix);
        Task<IEnumerable<string>> GetEmailsStartingWithAsync(string prefix);
    }
} 