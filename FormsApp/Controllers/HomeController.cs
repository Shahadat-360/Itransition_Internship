using System.Diagnostics;
using FormsApp.Data;
using FormsApp.Models;
using FormsApp.Services;
using FormsApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FormsApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly ISearchService _searchService;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            ISearchService searchService)
        {
            _logger = logger;
            _context = context;
            _searchService = searchService;
        }

        public async Task<IActionResult> Index()
        {
            // Cleanup any orphaned tags with zero usage
            await CleanupUnusedTags();
            
            var viewModel = new HomeViewModel
            {
                // Get only public templates
                PublicTemplates = await _context.FormTemplates
                    .Where(t => t.IsPublic)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(5)
                    .Include(t => t.Creator)
                    .Include(t => t.Comments)
                    .Select(t => new FormTemplateViewModel
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        ImageUrl = t.ImageUrl,
                        CreatorName = t.Creator.UserName,
                        CreatedAt = t.CreatedAt,
                        LikesCount = t.LikesCount,
                        CommentsCount = t.Comments.Count
                    })
                    .ToListAsync(),
                    
                // Get 5 most popular templates (by number of likes and comments)
                PopularTemplates = await _context.FormTemplates
                    .Where(t => t.IsPublic)
                    .Include(t => t.Responses)
                    .Include(t => t.Creator)
                    .Include(t => t.Likes)
                    .Include(t => t.Comments)
                    .OrderByDescending(t => t.LikesCount + t.Comments.Count)
                    .Take(5)
                    .Select(t => new FormTemplateViewModel
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        ImageUrl = t.ImageUrl,
                        CreatorName = t.Creator.UserName,
                        CreatedAt = t.CreatedAt,
                        LikesCount = t.LikesCount,
                        CommentsCount = t.Comments.Count
                    })
                    .ToListAsync(),
                    
                // Get most used tags for tag cloud
                TopTags = await _context.Tags
                    .Where(t => t.UsageCount > 0)
                    .Where(t => _context.TemplateTags
                        .Where(tt => tt.TagId == t.Id)
                        .Join(_context.FormTemplates.Where(ft => ft.IsPublic),
                            tt => tt.TemplateId,
                            ft => ft.Id,
                            (tt, ft) => ft)
                        .Any())
                    .OrderByDescending(t => t.UsageCount)
                    .Take(20)
                    .Select(t => new TagViewModel
                    {
                        Id = t.Id,
                        Name = t.Name,
                        UsageCount = t.UsageCount
                    })
                    .ToListAsync()
            };
            
            return View(viewModel);
        }
        
        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction(nameof(Index));
            }
            
            var templates = await _searchService.SearchTemplatesAsync(query);
            
            var viewModel = new SearchResultViewModel
            {
                SearchTerm = query,
                Templates = templates.Select(t => new FormTemplateViewModel
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    ImageUrl = t.ImageUrl,
                    CreatorName = t.Creator.UserName,
                    CreatedAt = t.CreatedAt,
                    LikesCount = t.LikesCount,
                    CommentsCount = t.Comments.Count
                }).ToList()
            };
            
            return View(viewModel);
        }
        
        [HttpGet]
        public async Task<IActionResult> SearchByTag(int? tagId, string tagName)
        {
            // Add debug logging
            Console.WriteLine($"SearchByTag called with tagId={tagId}, tagName={tagName}");
            
            Tag tag = null;
            
            try 
            {
                // Search by ID if provided
                if (tagId.HasValue)
                {
                    tag = await _context.Tags.FindAsync(tagId.Value);
                    Console.WriteLine($"Looking for tag by ID {tagId} - Found: {tag != null}");
                }
                // Search by name if provided
                else if (!string.IsNullOrWhiteSpace(tagName))
                {
                    // Use exact match (case-insensitive) for tag name
                    tag = await _context.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == tagName.ToLower());
                    Console.WriteLine($"Looking for tag by name '{tagName}' - Found: {tag != null}");
                }
                
                if (tag == null)
                {
                    // If both methods fail, try a more permissive search
                    if (!string.IsNullOrWhiteSpace(tagName))
                    {
                        Console.WriteLine($"Trying partial match for tag name '{tagName}'");
                        tag = await _context.Tags.FirstOrDefaultAsync(t => t.Name.ToLower().Contains(tagName.ToLower()));
                        Console.WriteLine($"Partial match result: {tag?.Name ?? "None found"}");
                    }
                }
                
                if (tag == null)
                {
                    // Direct debug for all available tags
                    var allTags = await _context.Tags.ToListAsync();
                    Console.WriteLine($"Available tags: {string.Join(", ", allTags.Select(t => $"{t.Id}:{t.Name}"))}");
                    
                    TempData["ErrorMessage"] = "Tag not found.";
                    return RedirectToAction(nameof(Index));
                }
                
                Console.WriteLine($"Found tag: {tag.Id} - {tag.Name}");
                
                var templateTagQuery = _context.TemplateTags
                    .Where(tt => tt.TagId == tag.Id)
                    .Include(tt => tt.Template)
                        .ThenInclude(t => t.Creator)
                    .Include(tt => tt.Template.Comments)
                    .Where(tt => tt.Template.IsPublic);
                
                // Debug the SQL query
                Console.WriteLine($"Query for template tags: {templateTagQuery.ToQueryString()}");
                
                var templates = await templateTagQuery
                    .Select(tt => tt.Template)
                    .ToListAsync();
                    
                Console.WriteLine($"Found {templates.Count} templates with tag '{tag.Name}'");
                
                var viewModel = new SearchResultViewModel
                {
                    SearchTerm = $"Tag: {tag.Name}",
                    Templates = templates.Select(t => new FormTemplateViewModel
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        ImageUrl = t.ImageUrl,
                        CreatorName = t.Creator.UserName,
                        CreatedAt = t.CreatedAt,
                        LikesCount = t.LikesCount,
                        CommentsCount = t.Comments.Count
                    }).ToList()
                };
                
                return View("Search", viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SearchByTag: {ex.Message}\n{ex.StackTrace}");
                TempData["ErrorMessage"] = "An error occurred while searching for templates by tag.";
                return RedirectToAction(nameof(Index));
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Helper method to remove any tags with zero usage count
        private async Task CleanupUnusedTags()
        {
            try
            {
                // Find all tags with zero usage count
                var unusedTags = await _context.Tags
                    .Where(t => t.UsageCount <= 0)
                    .ToListAsync();
                    
                if (unusedTags.Any())
                {
                    _logger.LogInformation($"Removing {unusedTags.Count} unused tags");
                    
                    // Remove them from the database
                    _context.Tags.RemoveRange(unusedTags);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cleaning up unused tags: {ex.Message}");
            }
        }
    }
} 