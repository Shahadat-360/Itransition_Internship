using FormsApp.Data;
using FormsApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FormsApp.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                var viewModel = new DashboardViewModel
                {
                    // Get user's templates
                    UserTemplates = await _context.FormTemplates
                        .Include(t => t.TopicNavigation)
                        .Where(t => t.CreatorId == userId)
                        .OrderByDescending(t => t.LastModifiedAt)
                        .Select(t => new FormTemplateViewModel
                        {
                            Id = t.Id,
                            Title = t.Title,
                            Description = t.Description,
                            TopicId = t.TopicId,
                            Topic = t.TopicNavigation != null ? t.TopicNavigation.Name : "Other",
                            ImageUrl = t.ImageUrl,
                            IsPublic = t.IsPublic,
                            CreatedAt = t.CreatedAt,
                            LikesCount = t.LikesCount
                        })
                        .Take(10)
                        .ToListAsync(),
                        
                    // Get user's form responses
                    UserResponses = await _context.FormResponses
                        .Where(r => r.RespondentId == userId)
                        .OrderByDescending(r => r.SubmittedAt)
                        .Select(r => new FormResponseViewModel
                        {
                            Id = r.Id,
                            TemplateId = r.TemplateId,
                            TemplateName = r.Template.Title,
                            CreatorName = r.Template.Creator.UserName,
                            SubmittedAt = r.SubmittedAt
                        })
                        .Take(10)
                        .ToListAsync()
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while loading the dashboard: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }
    }
} 