using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FormsApp.Data;
using FormsApp.Models;
using FormsApp.ViewModels;
using System.Security.Claims;
using FormsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json;

namespace FormsApp.Controllers
{
    public class FormTemplateController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISearchService _searchService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<FormTemplateController> _logger;

        public FormTemplateController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ISearchService searchService,
            IWebHostEnvironment webHostEnvironment,
            ILogger<FormTemplateController> logger)
        {
            _context = context;
            _userManager = userManager;
            _searchService = searchService;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }
        
        // GET: /FormTemplate
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                var templates = await _context.FormTemplates
                    .AsNoTracking()
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
                        LikesCount = t.LikesCount,
                        CommentsCount = _context.Comments.Count(c => c.TemplateId == t.Id)
                    })
                    .ToListAsync();
                    return View(templates);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error fetching form templates for user {UserId}", userId);
                TempData["ErrorMessage"] = "An error occurred while fetching your templates. Please try again.";
                return RedirectToAction("Index", "Home");
            }

        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Form template ID is required.";
                return RedirectToAction(nameof(Index));
            }

            var formTemplate = await _context.FormTemplates
                .Include(f => f.Creator)
                .Include(f => f.TopicNavigation)
                .Include(f => f.TemplateTags)
                    .ThenInclude(tt => tt.Tag)
                .Include(f => f.Questions)
                    .ThenInclude(q => q.Options)
                .Include(f => f.Likes)
                .Include(f => f.AllowedUsers)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (formTemplate == null)
            {
                TempData["ErrorMessage"] = "Form template not found.";
                return RedirectToAction(nameof(Index));
            }

            // Check if the current user has access to this template
            bool hasAccess = false;
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                var currentUserIsAdmin = User.IsInRole("Admin");

                // User has access if the template is public, they're the creator, an admin,
                // or they're in the allowed users list by ID or email
                hasAccess = formTemplate.IsPublic ||
                           currentUserIsAdmin ||
                           formTemplate.CreatorId == userId ||
                           formTemplate.AllowedUsers.Any(au => au.UserId == userId) ||
                           (!string.IsNullOrEmpty(userEmail) && formTemplate.AllowedUsers.Any(au => au.Email == userEmail));

                if (!hasAccess)
                {
                    TempData["ErrorMessage"] = "You don't have permission to view this template.";
                    return RedirectToAction(nameof(Index));
                }
            }
            else if (!formTemplate.IsPublic)
            {
                // If user is not authenticated and template is not public
                TempData["ErrorMessage"] = "You need to log in to view this template.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Details", "FormTemplate", new { id }) });
            }

            // Check if the current user has liked this template
            bool currentUserLiked = false;
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                currentUserLiked = formTemplate.Likes.Any(l => l.UserId == userId);
            }

            // Convert domain model to view model
            var viewModel = new FormTemplateViewModel
            {
                Id = formTemplate.Id,
                Title = formTemplate.Title,
                Description = formTemplate.Description,
                TopicId = formTemplate.TopicId,
                Topic = formTemplate.TopicNavigation?.Name ?? "Other",
                ImageUrl = formTemplate.ImageUrl,
                IsPublic = formTemplate.IsPublic,
                CreatedAt = formTemplate.CreatedAt,
                DateCreated = formTemplate.DateCreated,
                CreatorName = formTemplate.Creator.UserName,
                LikesCount = formTemplate.LikesCount,
                CurrentUserLiked = currentUserLiked,
                TagIds = formTemplate.TemplateTags.Select(tt => tt.TagId).ToList(),
                Tags = formTemplate.TemplateTags.Select(tt => tt.Tag.Name).ToList(),
                Questions = formTemplate.Questions.ToList()
            };

            // Add the required ViewData values
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserEmail = User.FindFirstValue(ClaimTypes.Email);
            var isAdmin = User.IsInRole("Admin");

            // Set CanEdit - user can edit if they are the creator or an admin
            ViewData["CanEdit"] = isAdmin || (currentUserId == formTemplate.CreatorId);

            // Set CanFill - user can fill if they are logged in and:
            // 1. The form is public, OR
            // 2. They created it, OR
            // 3. They are an admin, OR
            // 4. They are in the allowed users list by ID or email
            var isAllowedUser = formTemplate.AllowedUsers != null &&
                (formTemplate.AllowedUsers.Any(au => au.UserId == currentUserId) ||
                (!string.IsNullOrEmpty(currentUserEmail) && formTemplate.AllowedUsers.Any(au => au.Email == currentUserEmail)));

            ViewData["CanFill"] = User.Identity.IsAuthenticated &&
                (formTemplate.IsPublic || currentUserId == formTemplate.CreatorId || isAdmin || isAllowedUser);

            // Set the CreatorId for comment permissions
            ViewData["CreatorId"] = formTemplate.CreatorId;

            // Set Questions as QuestionViewModel list
            ViewData["Questions"] = formTemplate.Questions.Select(q => new QuestionViewModel
            {
                Id = q.Id,
                Text = q.Text,
                Description = q.Description,
                Type = q.Type,
                Order = q.Order,
                ShowInResults = q.ShowInResults,
                Options = q.Options.Select(o => new OptionViewModel
                {
                    Id = o.Id,
                    Text = o.Text,
                    Order = o.Order
                }).ToList()
            }).ToList();

            // Set Comments - we might need to fetch them first
            ViewData["Comments"] = await _context.Comments
                .Where(c => c.TemplateId == id)
                .Include(c => c.Author)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentViewModel
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    AuthorName = c.Author.UserName,
                    AuthorId = c.AuthorId,
                    TemplateId = c.TemplateId
                })
                .ToListAsync();

            return View(viewModel);
        }
        
        // GET: /FormTemplate/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            // Load topics for dropdown
            ViewData["Topics"] = await _context.Topics
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                .ToListAsync();
                
            ViewData["AllTags"] = _context.Tags.ToList();
            return View();
        }
        
        // POST: /FormTemplate/Create
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,TopicId,IsPublic,ImageFile,TagsJson,AllowedUserEmails")] FormTemplateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                var formTemplate = new FormTemplate
                {
                    Title = viewModel.Title,
                    Description = viewModel.Description,
                    TopicId = viewModel.TopicId,
                    IsPublic = viewModel.IsPublic,
                    DateCreated = DateTime.Now,
                    CreatorId = currentUserId
                };
                
                // Handle image upload
                if (viewModel.ImageFile != null && viewModel.ImageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "templates");
                    
                    // Create the directory if it doesn't exist
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    
                    // Generate a unique filename
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(viewModel.ImageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    // Save the file
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await viewModel.ImageFile.CopyToAsync(fileStream);
                    }
                    
                    // Save the relative URL to the database
                    formTemplate.ImageUrl = "/images/templates/" + uniqueFileName;
                }
                
                _context.Add(formTemplate);
                await _context.SaveChangesAsync();
                
                // Process tags from JSON
                if (!string.IsNullOrEmpty(viewModel.TagsJson))
                {
                    try 
                    {
                        var tagNames = JsonSerializer.Deserialize<List<string>>(viewModel.TagsJson);
                        
                        if (tagNames != null && tagNames.Any())
                        {
                            foreach (var tagName in tagNames)
                            {
                                await AddTagToTemplate(tagName, formTemplate.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing tags: {ex.Message}");
                    }
                }
                
                // Process allowed users if not public
                if (!formTemplate.IsPublic && viewModel.AllowedUserEmails != null)
                {
                    try
                    {
                        // Ensure AllowedUserEmails is correctly deserialized from JSON
                        // The input should already be a List<string> from model binding in most cases
                        List<string> emails = viewModel.AllowedUserEmails;
                        
                        // If not empty, process the emails
                        if (emails.Any())
                        {
                            await ProcessAllowedUsers(emails, formTemplate.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing allowed users: {ex.Message}");
                    }
                }
                
                TempData["SuccessMessage"] = "Form template created successfully.";
                return RedirectToAction(nameof(Details), new { id = formTemplate.Id });
            }
            
            // If we get here, something failed, redisplay form
            // Load topics for dropdown
            ViewData["Topics"] = await _context.Topics
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                .ToListAsync();
                
            ViewData["AllTags"] = _context.Tags.ToList();
            return View(viewModel);
        }
        
        // GET: /FormTemplate/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Form template ID is required.";
                return RedirectToAction(nameof(Index));
            }

            var formTemplate = await _context.FormTemplates
                .Include(f => f.TopicNavigation)
                .Include(f => f.TemplateTags)
                    .ThenInclude(f => f.Tag)
                .Include(f => f.Questions.OrderBy(q => q.Order))
                    .ThenInclude(q => q.Options.OrderBy(o => o.Order))
                .Include(f => f.AllowedUsers)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (formTemplate == null)
            {
                TempData["ErrorMessage"] = "Form template not found.";
                return RedirectToAction(nameof(Index));
            }
            
            // Check if the current user is the creator or an admin
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            
            if (!isAdmin && formTemplate.CreatorId != currentUserId)
            {
                TempData["ErrorMessage"] = "You don't have permission to edit this template.";
                return RedirectToAction(nameof(Details), new { id });
            }
            
            // Prepare view model for edit
            var viewModel = new FormTemplateViewModel
            {
                Id = formTemplate.Id,
                Title = formTemplate.Title,
                Description = formTemplate.Description,
                TopicId = formTemplate.TopicId,
                Topic = formTemplate.TopicNavigation?.Name ?? "Other",
                IsPublic = formTemplate.IsPublic,
                ImageUrl = formTemplate.ImageUrl,
                TagIds = formTemplate.TemplateTags.Select(tt => tt.TagId).ToList(),
                Tags = formTemplate.TemplateTags.Select(tt => tt.Tag.Name).ToList(),
                Questions = formTemplate.Questions.OrderBy(q => q.Order).ToList(),
                AllowedUserEmails = formTemplate.AllowedUsers
                    .Where(u => !string.IsNullOrEmpty(u.Email))
                    .Select(u => u.Email)
                    .ToList()
            };
            
            // Set the AllowedEmails string for the textarea
            if (viewModel.AllowedUserEmails.Any())
            {
                viewModel.AllowedEmails = string.Join(Environment.NewLine, viewModel.AllowedUserEmails);
            }
            
            // Debug tag loading
            Console.WriteLine($"Loading tags for template {formTemplate.Id}: {string.Join(", ", viewModel.Tags)}");
            Console.WriteLine($"Loading allowed users for template {formTemplate.Id}: {string.Join(", ", viewModel.AllowedUserEmails)}");
            
            // Load topics for dropdown
            ViewData["Topics"] = await _context.Topics
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                .ToListAsync();
                
            ViewData["AllTags"] = _context.Tags.ToList();
            return View(viewModel);
        }
        
        // POST: /FormTemplate/Edit/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,TopicId,IsPublic,TagsJson,AllowedEmails,ImageFile")] FormTemplateViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                TempData["ErrorMessage"] = "Invalid form template ID.";
                return RedirectToAction(nameof(Index));
            }

            // Debug incoming tag data
            Console.WriteLine($"Received TagsJson data for template {id}: {viewModel.TagsJson}");
            
            // Load the existing template with its tags
            var formTemplate = await _context.FormTemplates
                .Include(f => f.TemplateTags)
                    .ThenInclude(tt => tt.Tag)
                .Include(f => f.AllowedUsers)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (formTemplate == null)
            {
                TempData["ErrorMessage"] = "Form template not found.";
                return RedirectToAction(nameof(Index));
            }
            
            // Get existing tag names for logging
            var existingTagNames = formTemplate.TemplateTags.Select(tt => tt.Tag.Name).ToList();
            Console.WriteLine($"Existing tags in DB: {string.Join(", ", existingTagNames)}");
            
            // Process tags from the TagsJson field
            List<string> newTagNames = new List<string>();
            
            if (!string.IsNullOrEmpty(viewModel.TagsJson))
            {
                try
                {
                    // Deserialize and filter out empty/whitespace tags
                    newTagNames = JsonSerializer.Deserialize<List<string>>(viewModel.TagsJson)
                        ?.Where(t => !string.IsNullOrWhiteSpace(t))
                        ?.Select(t => t.Trim())
                        ?.ToList() ?? new List<string>();
                        
                    Console.WriteLine($"Deserialized tags from JSON: {string.Join(", ", newTagNames)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deserializing JSON tags: {ex.Message}");
                    // If error, use existing tags
                    newTagNames = existingTagNames;
                }
            }
            else
            {
                // If no tags provided, use existing tags
                newTagNames = existingTagNames;
                Console.WriteLine("No TagsJson provided, keeping existing tags");
            }
            
            // Process allowed emails string to list
            if (!string.IsNullOrEmpty(viewModel.AllowedEmails))
            {
                try
                {
                    // Try to parse as JSON first (from the hidden input field)
                    viewModel.AllowedUserEmails = JsonSerializer.Deserialize<List<string>>(viewModel.AllowedEmails);
                }
                catch
                {
                    // If not JSON, treat as newline/comma separated text (fallback for backward compatibility)
                    viewModel.AllowedUserEmails = viewModel.AllowedEmails
                        .Replace("\r\n", "\n")
                        .Replace("\r", "\n")
                        .Split(new[] { '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(e => e.Trim())
                        .Where(e => !string.IsNullOrWhiteSpace(e))
                        .ToList();
                }
            }
            else
            {
                viewModel.AllowedUserEmails = new List<string>();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if the current user is the creator or an admin
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var isAdmin = User.IsInRole("Admin");
                    
                    if (!isAdmin && formTemplate.CreatorId != currentUserId)
                    {
                        TempData["ErrorMessage"] = "You don't have permission to edit this template.";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                    
                    // Handle image upload
                    if (viewModel.ImageFile != null && viewModel.ImageFile.Length > 0)
                    {
                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(formTemplate.ImageUrl))
                        {
                            string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, formTemplate.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }
                        
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "templates");
                        
                        // Create the directory if it doesn't exist
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }
                        
                        // Generate a unique filename
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(viewModel.ImageFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        
                        // Save the file
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await viewModel.ImageFile.CopyToAsync(fileStream);
                        }
                        
                        // Save the relative URL to the database
                        formTemplate.ImageUrl = "/images/templates/" + uniqueFileName;
                    }
                    
                    // Update basic properties
                    formTemplate.Title = viewModel.Title;
                    formTemplate.Description = viewModel.Description;
                    formTemplate.TopicId = viewModel.TopicId;
                    formTemplate.IsPublic = viewModel.IsPublic;
                    formTemplate.LastModifiedAt = DateTime.UtcNow;

                    // Compare existing tags with new tags to determine what to add/remove
                    var existingTagSet = new HashSet<string>(existingTagNames.Select(t => t.ToLower()));
                    var newTagSet = new HashSet<string>(newTagNames.Select(t => t.ToLower()));
                    
                    // Get tags that need to be added
                    var tagsToAdd = newTagSet.Except(existingTagSet).ToList();
                    // Get tags that need to be removed
                    var tagsToRemove = existingTagSet.Except(newTagSet).ToList();
                    
                    // Log tag changes
                    if (tagsToAdd.Any())
                        Console.WriteLine($"Tags to add: {string.Join(", ", tagsToAdd)}");
                    if (tagsToRemove.Any())
                        Console.WriteLine($"Tags to remove: {string.Join(", ", tagsToRemove)}");
                    
                    // Only process tags if there are changes to make
                    if (tagsToAdd.Any() || tagsToRemove.Any())
                    {
                        Console.WriteLine("Updating tags to: " + string.Join(", ", newTagNames));
                        
                        // Remove tags that are no longer needed
                        foreach (var templateTag in formTemplate.TemplateTags.ToList())
                        {
                            if (tagsToRemove.Contains(templateTag.Tag.Name.ToLower()))
                            {
                                _context.TemplateTags.Remove(templateTag);
                                
                                // Update tag usage count
                                var tag = await _context.Tags.FindAsync(templateTag.TagId);
                                if (tag != null && tag.UsageCount > 0)
                                {
                                    tag.UsageCount--;
                                    Console.WriteLine($"Decremented tag '{tag.Name}' usage count to {tag.UsageCount}");
                                    
                                    if (tag.UsageCount == 0)
                                    {
                                        // If tag is no longer used, remove it
                                        _context.Tags.Remove(tag);
                                        Console.WriteLine($"Removed tag '{tag.Name}' because usage count is 0");
                                    }
                                    else
                                    {
                                        _context.Tags.Update(tag);
                                    }
                                }
                            }
                        }
                        
                        // Add new tags
                        foreach (var tagName in newTagNames)
                        {
                            // Check if this tag is already linked to the template (case insensitive)
                            bool exists = formTemplate.TemplateTags.Any(tt => 
                                tt.Tag.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));
                                
                            if (!exists)
                            {
                                await AddTagToTemplate(tagName, formTemplate.Id);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("No tag changes detected, keeping existing tags");
                    }
                    
                    // Process allowed users
                    // First, remove all existing allowed users
                    foreach (var allowedUser in formTemplate.AllowedUsers.ToList())
                    {
                        _context.TemplateAccessUsers.Remove(allowedUser);
                    }
                    
                    // Then add the new ones if not public
                    if (!formTemplate.IsPublic && viewModel.AllowedUserEmails != null && viewModel.AllowedUserEmails.Any())
                    {
                        await ProcessAllowedUsers(viewModel.AllowedUserEmails, formTemplate.Id);
                    }
                    
                    _context.Update(formTemplate);
                    await _context.SaveChangesAsync();
                    
                    // Load the updated tags for display in the next view
                    var updatedTags = await _context.TemplateTags
                        .Where(tt => tt.TemplateId == formTemplate.Id)
                        .Include(tt => tt.Tag)
                        .Select(tt => tt.Tag.Name)
                        .ToListAsync();
                    
                    Console.WriteLine($"Final tags after save: {JsonSerializer.Serialize(updatedTags)}");
                    
                    TempData["SuccessMessage"] = "Form template updated successfully.";
                    return RedirectToAction(nameof(Edit), new { id = viewModel.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TemplateExists(viewModel.Id))
                    {
                        TempData["ErrorMessage"] = "Form template not found.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            
            // If we get here, something failed, redisplay form
            // Reload topics for dropdown
            ViewData["Topics"] = await _context.Topics
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                .ToListAsync();
                
            ViewData["AllTags"] = _context.Tags.ToList();
            return View(viewModel);
        }
        
        // GET: /FormTemplate/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Form template ID is required.";
                return RedirectToAction(nameof(Index));
            }

            var formTemplate = await _context.FormTemplates
                .Include(f => f.Creator)
                .Include(f => f.TemplateTags)
                    .ThenInclude(tt => tt.Tag)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (formTemplate == null)
            {
                TempData["ErrorMessage"] = "Form template not found.";
                return RedirectToAction(nameof(Index));
            }
            
            // Check if the current user is the creator or an admin
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            
            if (!isAdmin && formTemplate.CreatorId != currentUserId)
            {
                TempData["ErrorMessage"] = "You don't have permission to delete this template.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Convert domain model to view model
            var viewModel = new FormTemplateViewModel
            {
                Id = formTemplate.Id,
                Title = formTemplate.Title,
                Description = formTemplate.Description,
                TopicId = formTemplate.TopicId,
                Topic = formTemplate.TopicNavigation?.Name ?? "Other",
                ImageUrl = formTemplate.ImageUrl,
                IsPublic = formTemplate.IsPublic,
                CreatedAt = formTemplate.CreatedAt,
                CreatorName = formTemplate.Creator.UserName,
                LikesCount = formTemplate.LikesCount,
                TagIds = formTemplate.TemplateTags.Select(tt => tt.TagId).ToList(),
                Tags = formTemplate.TemplateTags.Select(tt => tt.Tag.Name).ToList()
            };

            return View(viewModel);
        }
        
        // POST: /FormTemplate/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var formTemplate = await _context.FormTemplates
                    .Include(f => f.Questions)
                        .ThenInclude(q => q.Options)
                    .Include(f => f.Responses)
                        .ThenInclude(r => r.Answers)
                    .Include(f => f.TemplateTags)
                        .ThenInclude(tt => tt.Tag)
                    .FirstOrDefaultAsync(m => m.Id == id);
                    
                if (formTemplate == null)
                {
                    TempData["ErrorMessage"] = "Form template not found.";
                    return RedirectToAction(nameof(Index));
                }
                
                // Check if the current user is the creator or an admin
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");
                
                if (!isAdmin && formTemplate.CreatorId != currentUserId)
                {
                    TempData["ErrorMessage"] = "You don't have permission to delete this template.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                
                // Store tags for cleanup
                var tagsToUpdate = formTemplate.TemplateTags.Select(tt => tt.Tag).ToList();
                
                // Delete template image if exists
                if (!string.IsNullOrEmpty(formTemplate.ImageUrl))
                {
                    DeleteTemplateImageFile(formTemplate.ImageUrl);
                }
                
                // Delete all related entities
                // First, delete answers from responses
                foreach (var response in formTemplate.Responses)
                {
                    _context.Answers.RemoveRange(response.Answers);
                }
                
                // Then delete responses
                _context.FormResponses.RemoveRange(formTemplate.Responses);
                
                // Delete options from questions
                foreach (var question in formTemplate.Questions)
                {
                    _context.QuestionOptions.RemoveRange(question.Options);
                }
                
                // Delete questions
                _context.Questions.RemoveRange(formTemplate.Questions);
                
                // Also delete comments if any
                var comments = await _context.Comments.Where(c => c.TemplateId == id).ToListAsync();
                _context.Comments.RemoveRange(comments);
                
                // Remove template tags
                _context.TemplateTags.RemoveRange(formTemplate.TemplateTags);
                
                // Finally, delete the template
                _context.FormTemplates.Remove(formTemplate);
                
                // Update tag usage counts
                foreach (var tag in tagsToUpdate)
                {
                    // Decrement usage count
                    tag.UsageCount = Math.Max(0, tag.UsageCount - 1);
                    Console.WriteLine($"Decremented tag '{tag.Name}' usage count to {tag.UsageCount}");
                    
                    if (tag.UsageCount == 0)
                    {
                        // If tag is no longer used, remove it
                        _context.Tags.Remove(tag);
                        Console.WriteLine($"Tag '{tag.Name}' removed because usage count is zero");
                    }
                    else
                    {
                        _context.Tags.Update(tag);
                    }
                }
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                TempData["SuccessMessage"] = "Form template deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting form template {TemplateId}", id);
                TempData["ErrorMessage"] = $"An error occurred while deleting the template: {ex.Message}";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }
        
        // GET: /FormTemplate/AllTemplates
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AllTemplates()
        {
            var templates = await _context.FormTemplates
                .Include(t => t.Creator)
                .Include(t => t.TopicNavigation)
                .OrderByDescending(t => t.LastModifiedAt)
                .ToListAsync();
                
            var viewModels = templates.Select(t => new FormTemplateViewModel
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                TopicId = t.TopicId,
                Topic = t.TopicNavigation != null ? t.TopicNavigation.Name : "Other",
                ImageUrl = t.ImageUrl,
                IsPublic = t.IsPublic,
                CreatedAt = t.CreatedAt,
                CreatorName = t.Creator.UserName,
                LikesCount = t.LikesCount
            }).ToList();
                
            return View(viewModels);
        }
        
        // POST: /FormTemplate/ToggleLike
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLike(int id)
        {
            // Get the current user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }
            
            // Find the template
            var template = await _context.FormTemplates
                .FirstOrDefaultAsync(t => t.Id == id);
                
            if (template == null)
            {
                TempData["ErrorMessage"] = "Template not found.";
                return RedirectToAction(nameof(Index));
            }
            
            // Check if user already liked this template
            var existingLike = await _context.Set<TemplateLike>()
                .FirstOrDefaultAsync(tl => tl.TemplateId == id && tl.UserId == userId);
                
            if (existingLike != null)
            {
                // User already liked, so remove the like
                _context.Remove(existingLike);
                template.LikesCount = Math.Max(0, template.LikesCount - 1); // Ensure count doesn't go below 0
                TempData["InfoMessage"] = "Like removed.";
            }
            else
            {
                // User hasn't liked, so add a new like
                var like = new TemplateLike
                {
                    TemplateId = id,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Add(like);
                template.LikesCount++;
                TempData["SuccessMessage"] = "Template liked!";
            }
            
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Details), new { id });
        }
        
        // POST: /FormTemplate/AddComment
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int templateId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Comment cannot be empty.";
                return RedirectToAction(nameof(Details), new { id = templateId });
            }

            var template = await _context.FormTemplates.FindAsync(templateId);
            if (template == null)
            {
                TempData["ErrorMessage"] = "Form template not found.";
                return RedirectToAction(nameof(Index));
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var comment = new Comment
            {
                Content = content,
                CreatedAt = DateTime.UtcNow,
                TemplateId = templateId,
                AuthorId = currentUserId
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comment added successfully.";
            return RedirectToAction(nameof(Details), new { id = templateId });
        }
        
        // POST: /FormTemplate/EditComment
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditComment(int commentId, string content, int templateId)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Comment cannot be empty.";
                return RedirectToAction(nameof(Details), new { id = templateId });
            }

            var comment = await _context.Comments
                .Include(c => c.Template)
                .FirstOrDefaultAsync(c => c.Id == commentId);
                
            if (comment == null)
            {
                TempData["ErrorMessage"] = "Comment not found.";
                return RedirectToAction(nameof(Details), new { id = templateId });
            }

            // Check if the current user is authorized to edit this comment
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            
            if (!isAdmin && comment.AuthorId != currentUserId && comment.Template.CreatorId != currentUserId)
            {
                TempData["ErrorMessage"] = "You don't have permission to edit this comment.";
                return RedirectToAction(nameof(Details), new { id = templateId });
            }

            // Update the comment content
            comment.Content = content;
            _context.Comments.Update(comment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comment updated successfully.";
            return RedirectToAction(nameof(Details), new { id = templateId });
        }
        
        // POST: /FormTemplate/DeleteComment
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int commentId, int templateId)
        {
            var comment = await _context.Comments
                .Include(c => c.Template)
                .FirstOrDefaultAsync(c => c.Id == commentId);
                
            if (comment == null)
            {
                TempData["ErrorMessage"] = "Comment not found.";
                return RedirectToAction(nameof(Details), new { id = templateId });
            }

            // Check if the current user is authorized to delete this comment
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            
            if (!isAdmin && comment.AuthorId != currentUserId && comment.Template.CreatorId != currentUserId)
            {
                TempData["ErrorMessage"] = "You don't have permission to delete this comment.";
                return RedirectToAction(nameof(Details), new { id = templateId });
            }

            // Delete the comment
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comment deleted successfully.";
            return RedirectToAction(nameof(Details), new { id = templateId });
        }
        
        private bool TemplateExists(int id)
        {
            return _context.FormTemplates.Any(e => e.Id == id);
        }
        
        // POST: /FormTemplate/BatchDelete
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BatchDelete(List<int> selectedTemplates, string returnUrl)
        {
            if (selectedTemplates == null || !selectedTemplates.Any())
            {
                TempData["InfoMessage"] = "No templates were selected for deletion.";
                return string.IsNullOrEmpty(returnUrl) ? RedirectToAction(nameof(Index)) : Redirect(returnUrl);
            }
            
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");
                
                int deletedCount = 0;
                List<string> errors = new List<string>();
                Dictionary<int, Tag> tagsToUpdate = new Dictionary<int, Tag>();
                
                foreach (var templateId in selectedTemplates)
                {
                    var template = await _context.FormTemplates
                        .Include(f => f.Questions)
                            .ThenInclude(q => q.Options)
                        .Include(f => f.Responses)
                            .ThenInclude(r => r.Answers)
                        .Include(f => f.TemplateTags)
                            .ThenInclude(tt => tt.Tag)
                        .FirstOrDefaultAsync(t => t.Id == templateId);
                        
                    if (template == null)
                    {
                        continue; // Skip if template doesn't exist
                    }
                    
                    // Check if the current user is the creator or an admin
                    if (!isAdmin && template.CreatorId != currentUserId)
                    {
                        errors.Add($"You don't have permission to delete template: {template.Title}");
                        continue;
                    }
                    
                    try
                    {
                        // Store tags from this template for cleanup
                        foreach (var tt in template.TemplateTags)
                        {
                            var tag = tt.Tag;
                            if (!tagsToUpdate.ContainsKey(tag.Id))
                            {
                                tagsToUpdate.Add(tag.Id, tag);
                                // Initialize with current usage count
                                tag.UsageCount = tag.UsageCount;
                            }
                            else
                            {
                                // For each appearance of the tag, we need to decrement later
                                // We don't modify the actual tag count yet, just track it
                            }
                        }
                        
                        // Delete template image if exists
                        if (!string.IsNullOrEmpty(template.ImageUrl))
                        {
                            DeleteTemplateImageFile(template.ImageUrl);
                        }
                        
                        // Delete all related entities
                        // First, delete answers from responses
                        foreach (var response in template.Responses)
                        {
                            _context.Answers.RemoveRange(response.Answers);
                        }
                        
                        // Then delete responses
                        _context.FormResponses.RemoveRange(template.Responses);
                        
                        // Delete options from questions
                        foreach (var question in template.Questions)
                        {
                            _context.QuestionOptions.RemoveRange(question.Options);
                        }
                        
                        // Delete questions
                        _context.Questions.RemoveRange(template.Questions);
                        
                        // Also delete comments if any
                        var comments = await _context.Comments.Where(c => c.TemplateId == templateId).ToListAsync();
                        _context.Comments.RemoveRange(comments);
                        
                        // Remove template tags
                        _context.TemplateTags.RemoveRange(template.TemplateTags);
                        
                        // Finally, delete the template
                        _context.FormTemplates.Remove(template);
                        
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting template {templateId}: {ex.Message}");
                        errors.Add($"Error deleting template: {template.Title}. Reason: {ex.Message}");
                    }
                }
                
                // Update tag usage counts
                foreach (var tagEntry in tagsToUpdate)
                {
                    var tag = tagEntry.Value;
                    // Get the actual count of template tags being deleted with this tag
                    int tagDeletedCount = 0;
                    
                    foreach (var templateId in selectedTemplates)
                    {
                        // Check if this template had this tag
                        var hasTag = await _context.TemplateTags
                            .AnyAsync(tt => tt.TemplateId == templateId && tt.TagId == tag.Id);
                            
                        if (hasTag)
                        {
                            tagDeletedCount++;
                        }
                    }
                    
                    if (tagDeletedCount > 0)
                    {
                        // Decrement tag usage count by the number of templates with this tag
                        tag.UsageCount = Math.Max(0, tag.UsageCount - tagDeletedCount);
                        Console.WriteLine($"Decremented tag '{tag.Name}' usage count by {tagDeletedCount} to {tag.UsageCount}");
                        
                        if (tag.UsageCount == 0)
                        {
                            // If tag is no longer used, remove it
                            _context.Tags.Remove(tag);
                            Console.WriteLine($"Tag '{tag.Name}' removed because usage count is zero");
                        }
                        else
                        {
                            _context.Tags.Update(tag);
                        }
                    }
                }
                
                // Save all changes at once
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                if (deletedCount > 0)
                {
                    TempData["Success"] = $"Successfully deleted {deletedCount} template{(deletedCount != 1 ? "s" : "")}.";
                }
                
                if (errors.Any())
                {
                    TempData["Error"] = string.Join("<br>", errors);
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"General error in batch delete: {ex.Message}");
                TempData["Error"] = $"An error occurred: {ex.Message}";
            }
            
            return string.IsNullOrEmpty(returnUrl) ? RedirectToAction(nameof(Index)) : Redirect(returnUrl);
        }

        // Helper method to add a tag by name to a template
        private async Task AddTagToTemplate(string tagName, int templateId)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                Console.WriteLine($"Skipping empty tag for template {templateId}");
                return;
            }
            
            // Normalize tag name by trimming
            tagName = tagName.Trim();
            Console.WriteLine($"Adding tag '{tagName}' to template {templateId}");
            
            try
            {
                // Check if tag already exists (case insensitive)
                var tag = await _context.Tags
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == tagName.ToLower());
                
                // Create tag if it doesn't exist
                if (tag == null)
                {
                    Console.WriteLine($"Creating new tag '{tagName}'");
                    tag = new Tag { Name = tagName, UsageCount = 1 };
                    _context.Tags.Add(tag);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Created tag with ID {tag.Id}");
                }
                else
                {
                    Console.WriteLine($"Using existing tag '{tagName}' with ID {tag.Id}");
                    // Increment usage count if tag exists
                    tag.UsageCount++;
                    _context.Tags.Update(tag);
                }
                
                // Check if this tag is already linked to the template (case insensitive)
                var existingTemplateTag = await _context.TemplateTags
                    .FirstOrDefaultAsync(tt => tt.TagId == tag.Id && tt.TemplateId == templateId);
                    
                if (existingTemplateTag == null)
                {
                    Console.WriteLine($"Creating new template-tag link between template {templateId} and tag {tag.Id}");
                    // Create link between template and tag
                    var templateTag = new TemplateTag
                    {
                        TagId = tag.Id,
                        TemplateId = templateId
                    };
                    
                    _context.TemplateTags.Add(templateTag);
                }
                else
                {
                    Console.WriteLine($"Template {templateId} already has tag {tag.Id}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding tag '{tagName}' to template {templateId}: {ex.Message}");
                // Don't throw - we'll handle failures gracefully and continue with other tags
            }
        }
        
        // Helper method to process allowed users for a template
        private async Task ProcessAllowedUsers(List<string> emails, int templateId)
        {
            if (emails == null || !emails.Any())
                return;
                
            foreach (var email in emails.Where(e => !string.IsNullOrWhiteSpace(e)))
            {
                // Check if this is a valid email
                if (!IsValidEmail(email))
                    continue;
                    
                try
                {
                    // Try to find user by email
                    var user = await _userManager.FindByEmailAsync(email);
                    
                    if (user != null)
                    {
                        // User exists, add by user ID
                        var templateAccess = new TemplateAccessUser
                        {
                            TemplateId = templateId,
                            UserId = user.Id,
                            Email = email,
                            AddedAt = DateTime.UtcNow
                        };
                        
                        _context.TemplateAccessUsers.Add(templateAccess);
                    }
                    else
                    {
                        // User doesn't exist yet, store by email only without linking to a user
                        var templateAccess = new TemplateAccessUser
                        {
                            TemplateId = templateId,
                            Email = email,
                            AddedAt = DateTime.UtcNow
                        };
                        
                        _context.TemplateAccessUsers.Add(templateAccess);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue processing other emails
                    Console.WriteLine($"Error processing email {email}: {ex.Message}");
                }
            }
            
            await _context.SaveChangesAsync();
        }
        
        // Helper method to validate email
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // GET: /FormTemplate/SharedTemplates
        [Authorize]
        public async Task<IActionResult> SharedTemplates()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            
            var sharedTemplates = await _context.FormTemplates
                .Include(t => t.Creator)
                .Include(t => t.TopicNavigation)
                .Include(t => t.AllowedUsers)
                .Include(t => t.Comments)
                // Get templates shared with this user (by ID or email) but not created by them
                .Where(t => t.CreatorId != userId && 
                            (t.AllowedUsers.Any(au => au.UserId == userId) ||
                             (!string.IsNullOrEmpty(userEmail) && t.AllowedUsers.Any(au => au.Email == userEmail))))
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
                    CreatorName = t.Creator.UserName,
                    LikesCount = t.LikesCount,
                    CommentsCount = t.Comments.Count
                })
                .ToListAsync();
                
            return View(sharedTemplates);
        }

        private void DeleteTemplateImageFile(string imageUrl)
        {
            // Construct the full file path
            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, imageUrl.TrimStart('/'));

            // Check if the file exists
            if (System.IO.File.Exists(filePath))
            {
                // Delete the file
                System.IO.File.Delete(filePath);
            }
        }

        // GET: /FormTemplate/DeleteImage/5
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var formTemplate = await _context.FormTemplates.FindAsync(id);
                
            if (formTemplate == null)
            {
                TempData["ErrorMessage"] = "Form template not found.";
                return RedirectToAction(nameof(Index));
            }
            
            // Check if the current user is the creator or an admin
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            
            if (!isAdmin && formTemplate.CreatorId != currentUserId)
            {
                TempData["ErrorMessage"] = "You don't have permission to edit this template.";
                return RedirectToAction(nameof(Details), new { id });
            }
            
            if (string.IsNullOrEmpty(formTemplate.ImageUrl))
            {
                TempData["InfoMessage"] = "No image to delete.";
                return RedirectToAction(nameof(Edit), new { id });
            }
            
            try
            {
                // Delete the image file
                DeleteTemplateImageFile(formTemplate.ImageUrl);
                
                // Remove the image URL from the template
                formTemplate.ImageUrl = null;
                
                // Save the changes
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Image deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting image: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Edit), new { id });
        }

        // GET: /FormTemplate/SearchEmails?term={searchTerm}
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> SearchEmails(string term)
        {
            var emails = await _searchService.GetEmailsStartingWithAsync(term);
            return Json(emails);
        }
    }
} 