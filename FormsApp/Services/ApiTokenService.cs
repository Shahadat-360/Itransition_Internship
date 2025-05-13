using FormsApp.Data;
using FormsApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace FormsApp.Services
{
    public class ApiTokenService : IApiTokenService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApiTokenService> _logger;

        public ApiTokenService(ApplicationDbContext context, ILogger<ApiTokenService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> GenerateTokenForUserAsync(string userId)
        {
            // Generate a secure random token
            var tokenBytes = new byte[32]; // 256 bits
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            
            // Convert to Base64 string (URL-safe)
            var token = Convert.ToBase64String(tokenBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");

            // Check if user already has a token
            var existingToken = await _context.ApiTokens
                .FirstOrDefaultAsync(t => t.UserId == userId && t.IsActive);

            if (existingToken != null)
            {
                // Update existing token
                existingToken.Token = token;
                existingToken.CreatedAt = DateTime.UtcNow;
                existingToken.LastUsed = null;
            }
            else
            {
                // Create new token
                var apiToken = new ApiToken
                {
                    UserId = userId,
                    Token = token,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                
                _context.ApiTokens.Add(apiToken);
            }
            
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            var apiToken = await _context.ApiTokens
                .FirstOrDefaultAsync(t => t.Token == token && t.IsActive);

            if (apiToken == null)
                return false;

            // Update last used timestamp
            apiToken.LastUsed = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            return true;
        }

        public async Task<string?> GetUserIdFromTokenAsync(string token)
        {
            var apiToken = await _context.ApiTokens
                .FirstOrDefaultAsync(t => t.Token == token && t.IsActive);

            return apiToken?.UserId;
        }

        public async Task<ApiToken?> GetTokenByValueAsync(string token)
        {
            return await _context.ApiTokens
                .FirstOrDefaultAsync(t => t.Token == token && t.IsActive);
        }

        public async Task<IEnumerable<ApiToken>> GetTokensByUserIdAsync(string userId)
        {
            return await _context.ApiTokens
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> RevokeTokenAsync(int tokenId, string userId)
        {
            var token = await _context.ApiTokens
                .FirstOrDefaultAsync(t => t.Id == tokenId && t.UserId == userId);

            if (token == null)
                return false;

            token.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }
    }
} 