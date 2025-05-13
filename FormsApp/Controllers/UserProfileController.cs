using FormsApp.Models;
using FormsApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FormsApp.Services;
using Microsoft.EntityFrameworkCore;
using FormsApp.Data;

namespace FormsApp.Controllers
{
    [Authorize]
    public class UserProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserProfileController> _logger;
        private readonly ISalesforceService _salesforceService;
        private readonly ApplicationDbContext _context;
        private readonly IApiTokenService _apiTokenService;

        public UserProfileController(
            UserManager<ApplicationUser> userManager,
            ILogger<UserProfileController> logger,
            ISalesforceService salesforceService,
            ApplicationDbContext context,
            IApiTokenService apiTokenService)
        {
            _userManager = userManager;
            _logger = logger;
            _salesforceService = salesforceService;
            _context = context;
            _apiTokenService = apiTokenService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> ApiToken()
        {
            var user = await _userManager.GetUserAsync(User);
            var tokens = await _apiTokenService.GetTokensByUserIdAsync(user.Id);
            var activeToken = tokens.FirstOrDefault(t => t.IsActive)?.Token;
            
            var viewModel = new ApiTokenViewModel
            {
                ApiToken = activeToken,
                HasActiveToken = !string.IsNullOrEmpty(activeToken),
                ApiEndpointBaseUrl = $"{Request.Scheme}://{Request.Host}/api/v1"
            };
            
            return View(viewModel);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateApiToken()
        {
            var user = await _userManager.GetUserAsync(User);
            
            try
            {
                var token = await _apiTokenService.GenerateTokenForUserAsync(user.Id);
                TempData["SuccessMessage"] = "New API token generated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating API token for user {UserId}", user.Id);
                TempData["ErrorMessage"] = "There was an error generating your API token.";
            }
            
            return RedirectToAction(nameof(ApiToken));
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeApiToken(int tokenId)
        {
            var user = await _userManager.GetUserAsync(User);
            
            try
            {
                var result = await _apiTokenService.RevokeTokenAsync(tokenId, user.Id);
                if (result)
                {
                    TempData["SuccessMessage"] = "API token revoked successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Could not find the specified token.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking API token for user {UserId}", user.Id);
                TempData["ErrorMessage"] = "There was an error revoking your API token.";
            }
            
            return RedirectToAction(nameof(ApiToken));
        }

        [HttpGet]
        public async Task<IActionResult> SalesforceIntegration()
        {
            var user = await _userManager.GetUserAsync(User);

            // Check if user already has Salesforce integration
            var isIntegrated = await _context.SalesforceUserProfiles
                .AnyAsync(p => p.UserId == user.Id);

            if (isIntegrated)
            {
                TempData["InfoMessage"] = "Your account is already connected to our CRM system.";
                return RedirectToAction("Index");
            }

            var viewModel = new SalesforceProfileViewModel();

            // Pre-fill with user data if available
            if (User.Identity.IsAuthenticated)
            {
                var email = User.FindFirstValue(ClaimTypes.Email);
                viewModel.Email = email;
                ViewData["EmailReadOnly"] = true;  // Indicate that email is read-only
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SalesforceIntegration(SalesforceProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);

            // Check if user already has Salesforce integration
            var isIntegrated = await _context.SalesforceUserProfiles
                .AnyAsync(p => p.UserId == user.Id);

            if (isIntegrated)
            {
                TempData["InfoMessage"] = "Your account is already connected to our CRM system.";
                return RedirectToAction("Index");
            }

            try
            {
                // Start a transaction
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create Salesforce Account
                    var account = new SalesforceAccount
                    {
                        Name = model.CompanyName,
                        Website = model.Website,
                        Industry = model.Industry,
                        Description = model.CompanyDescription,
                        Phone = model.BusinessPhone,
                        City = model.City,
                        Country = model.Country
                    };

                    var accountId = await _salesforceService.CreateAccountAsync(account);

                    // Create Salesforce Contact linked to the Account
                    var contact = new SalesforceContact
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        Phone = model.Phone,
                        Title = model.JobTitle,
                        Department = model.Department,
                        AccountId = accountId
                    };

                    var contactId = await _salesforceService.CreateContactAsync(contact);

                    // Save the integration record
                    var salesforceProfile = new SalesforceUserProfile
                    {
                        UserId = user.Id,
                        IntegrationDate = DateTime.UtcNow,
                        SalesforceAccountId = accountId,
                        SalesforceContactId = contactId
                    };

                    _context.SalesforceUserProfiles.Add(salesforceProfile);
                    await _context.SaveChangesAsync();

                    // Commit the transaction
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Your information has been successfully added to our CRM system.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Rollback the transaction on error
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "There was an error processing your request. Please try again later.";
                    _logger.LogError(ex, "Error during Salesforce integration for user {UserId}", user.Id);
                    throw; // Re-throw to be caught by outer exception handler
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Salesforce Account/Contact for user {UserName}", User.Identity.Name);
                ModelState.AddModelError("", "There was an error processing your request. Please try again later.");
                return View(model);
            }
        }
    }
}