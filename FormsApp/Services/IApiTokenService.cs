using FormsApp.Models;

namespace FormsApp.Services
{
    public interface IApiTokenService
    {
        Task<string> GenerateTokenForUserAsync(string userId);
        Task<bool> ValidateTokenAsync(string token);
        Task<string?> GetUserIdFromTokenAsync(string token);
        Task<ApiToken?> GetTokenByValueAsync(string token);
        Task<IEnumerable<ApiToken>> GetTokensByUserIdAsync(string userId);
        Task<bool> RevokeTokenAsync(int tokenId, string userId);
    }
}
