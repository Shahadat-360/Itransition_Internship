using FormsApp.Models;

namespace FormsApp.Services
{
    public interface ISalesforceService
    {
        Task<string> AuthenticateAsync();
        Task<string> CreateAccountAsync(SalesforceAccount account);
        Task<string> CreateContactAsync(SalesforceContact contact);
    }
}