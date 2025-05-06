using FormsApp.Data;
using FormsApp.Models;
using FormsApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace FormsApp.Controllers
{
    [Authorize]
    public class QuestionController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        public QuestionController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        // GET: /Question/Index?id=X
        public async Task<IActionResult> Index(int id)
        {
            try
            {
                // Get the template
                var template = await _context.FormTemplates
                    .Include(t => t.AllowedUsers)
                    .FirstOrDefaultAsync(t => t.Id == id);
                    
                if (template == null)
                {
                    TempData["Error"] = "Template not found.";
                    return RedirectToAction("Index", "FormTemplate");
                }
                
                // Check if the current user has access to this template
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var isAdmin = User.IsInRole("Admin");
                
                // User has access if they're an admin, the creator, or in the allowed users by ID or email
                var hasAccess = isAdmin || 
                               template.CreatorId == userId || 
                               template.AllowedUsers.Any(au => au.UserId == userId) ||
                               (!string.IsNullOrEmpty(userEmail) && template.AllowedUsers.Any(au => au.Email == userEmail));
                
                if (!hasAccess)
                {
                    TempData["Error"] = "You don't have permission to edit this template.";
                    return RedirectToAction("Index", "FormTemplate");
                }
                
                // Get questions for the template
                var questions = await _context.Questions
                    .Include(q => q.Options)
                    .Where(q => q.TemplateId == id)
                    .Select(q => new QuestionViewModel
                    {
                        Id = q.Id,
                        Text = q.Text,
                        Description = q.Description,
                        Type = q.Type,
                        Required = q.Required,
                        ShowInResults = q.ShowInResults,
                        TemplateId = q.TemplateId,
                        Order = q.Order,
                        Options = q.Options.Select(o => new OptionViewModel
                        {
                            Id = o.Id,
                            Text = o.Text,
                            Order = o.Order,
                            QuestionId = o.QuestionId
                        }).ToList()
                    })
                    .ToListAsync();
                    
                ViewBag.TemplateId = id;
                ViewBag.TemplateTitle = template.Title;
                
                return View(questions);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index", "FormTemplate");
            }
        }
        
        // GET: /Question/Create
        public async Task<IActionResult> Create(int id)
        {
            try
            {
                // Check if the template exists
                var template = await _context.FormTemplates.FindAsync(id);
                if (template == null)
                {
                    TempData["ErrorMessage"] = "Template not found.";
                    return RedirectToAction("Index", "FormTemplate");
                }
                
                // Check if the user has permission to edit the template
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");
                
                if (!isAdmin && template.CreatorId != currentUserId)
                {
                    TempData["ErrorMessage"] = "You don't have permission to add questions to this template.";
                    return RedirectToAction("Details", "FormTemplate", new { id });
                }
                
                // Create an empty question model
                var model = new QuestionViewModel
                {
                    TemplateId = id,
                    ShowInResults = true, // Set default value for checkbox
                    Options = new List<OptionViewModel>()
                };
                
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while creating the question: " + ex.Message;
                return RedirectToAction("Index", "FormTemplate");
            }
        }
        
        // POST: /Question/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QuestionViewModel model)
        {
            try
            {
                // For single and multiline text questions, we don't need options
                // Clear any ModelState errors related to Options
                if (model.Type == QuestionType.SingleLineText || 
                    model.Type == QuestionType.MultiLineText || 
                    model.Type == QuestionType.Integer)
                {
                    // Remove any options validation errors for non-option question types
                    foreach (var key in ModelState.Keys.ToList())
                    {
                        if (key.StartsWith("Options"))
                        {
                            ModelState.Remove(key);
                        }
                    }
                }
                
                // Remove Description validation errors as it should be optional
                if (ModelState.ContainsKey("Description"))
                {
                    ModelState.Remove("Description");
                }
                
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    
                    // Use the standard error message from validation
                    TempData["Error"] = $"Validation failed: {errors}";
                    // Redirect back to the Index view which contains the form
                    return RedirectToAction("Index", new { id = model.TemplateId }); 
                }
                
                // Check if the template exists
                var template = await _context.FormTemplates
                    .Include(t => t.AllowedUsers)
                    .FirstOrDefaultAsync(t => t.Id == model.TemplateId);
                    
                if (template == null)
                {
                    TempData["Error"] = "Template not found.";
                    return RedirectToAction("Index", "FormTemplate");
                }
                
                // Check if the current user has access to edit this template
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var isAdmin = User.IsInRole("Admin");
                
                // User has access if they're an admin, the creator, or in the allowed users by ID or email
                var hasAccess = isAdmin || 
                               template.CreatorId == userId || 
                               template.AllowedUsers.Any(au => au.UserId == userId) ||
                               (!string.IsNullOrEmpty(userEmail) && template.AllowedUsers.Any(au => au.Email == userEmail));
                
                if (!hasAccess)
                {
                    TempData["Error"] = "You don't have permission to edit this template.";
                    return RedirectToAction("Index", "FormTemplate");
                }
                
                // Get the highest order value for this template
                var highestOrder = await _context.Questions
                    .Where(q => q.TemplateId == model.TemplateId)
                    .Select(q => (int?)q.Order)
                    .MaxAsync() ?? 0;
                
                // Create the new question
                var question = new Question
                {
                    Text = model.Text,
                    Description = model.Description,
                    Type = model.Type,
                    Required = model.Required,
                    ShowInResults = model.ShowInResults,
                    TemplateId = model.TemplateId,
                    Order = highestOrder + 1
                };
                
                _context.Questions.Add(question);
                await _context.SaveChangesAsync();

                // If question type is MultipleChoice or Poll, add the options
                if (model.Type == QuestionType.MultipleChoice || model.Type == QuestionType.Poll)
                {
                    if (model.Options != null && model.Options.Any())
                    {
                        int order = 0;
                        foreach (var option in model.Options.Where(o => !string.IsNullOrWhiteSpace(o.Text)))
                        {
                            var questionOption = new QuestionOption
                            {
                                Text = option.Text,
                                Order = order++,
                                QuestionId = question.Id
                            };
                            _context.QuestionOptions.Add(questionOption);
                        }
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["Success"] = "Question created successfully.";
                return RedirectToAction("Index", new { id = model.TemplateId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index", new { id = model.TemplateId });
            }
        }
        
        // GET: /Question/Edit
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var question = await _context.Questions
                    .Include(q => q.Template)
                    .Include(q => q.Options)
                    .FirstOrDefaultAsync(q => q.Id == id);
                    
                if (question == null)
                {
                    TempData["Error"] = "Question not found.";
                    return RedirectToAction("Index", "FormTemplate");
                }
                
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");
                
                // Check if the current user can edit this template
                if (!isAdmin && question.Template.CreatorId != currentUserId)
                {
                    TempData["Error"] = "You don't have permission to edit this question.";
                    return RedirectToAction(nameof(Index), new { templateId = question.TemplateId });
                }
                
                var model = new QuestionViewModel
                {
                    Id = question.Id,
                    Text = question.Text,
                    Description = question.Description,
                    Type = question.Type,
                    Order = question.Order,
                    ShowInResults = question.ShowInResults,
                    TemplateId = question.TemplateId,
                    Options = question.Options.OrderBy(o => o.Order)
                        .Select(o => new OptionViewModel 
                        { 
                            Id = o.Id, 
                            Text = o.Text, 
                            Order = o.Order,
                            QuestionId = o.QuestionId
                        }).ToList()
                };
                
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index", "FormTemplate");
            }
        }
        
        // POST: /Question/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(QuestionViewModel model)
        {
            try
            {
                // Remove Description validation errors as it should be optional
                if (ModelState.ContainsKey("Description"))
                {
                    ModelState.Remove("Description");
                }
                
                // Check if the question exists
                var question = await _context.Questions
                    .Include(q => q.Template)
                    .ThenInclude(t => t.AllowedUsers)
                    .FirstOrDefaultAsync(q => q.Id == model.Id);
                    
                if (question == null)
                {
                    TempData["Error"] = "Question not found.";
                    return RedirectToAction("Index", new { id = model.TemplateId });
                }
                
                // Check if the current user has access to edit this template
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var isAdmin = User.IsInRole("Admin");
                
                // User has access if they're an admin, the creator, or in the allowed users by ID or email
                var hasAccess = isAdmin || 
                               question.Template.CreatorId == userId || 
                               question.Template.AllowedUsers.Any(au => au.UserId == userId) ||
                               (!string.IsNullOrEmpty(userEmail) && question.Template.AllowedUsers.Any(au => au.Email == userEmail));
                
                if (!hasAccess)
                {
                    TempData["Error"] = "You don't have permission to edit this template.";
                    return RedirectToAction("Index", "FormTemplate");
                }
                
                // Update question
                question.Text = model.Text;
                question.Description = model.Description;
                question.ShowInResults = model.ShowInResults;
                
                _context.Update(question);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Question updated successfully.";
                return RedirectToAction("Index", new { id = model.TemplateId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index", new { id = model.TemplateId });
            }
        }
        
        // POST: /Question/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                // Get the question with its template and answers
                var question = await _context.Questions
                    .Include(q => q.Template)
                    .ThenInclude(t => t.AllowedUsers)
                    .Include(q => q.Answers) // Include answers to delete them first
                    .FirstOrDefaultAsync(q => q.Id == id);
                    
                if (question == null)
                {
                    TempData["Error"] = "Question not found.";
                    return RedirectToAction("Index", "FormTemplate");
                }
                
                // Get the template ID for redirection
                var templateId = question.TemplateId;
                
                // Check if the current user has access to edit this template
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var isAdmin = User.IsInRole("Admin");
                
                // User has access if they're an admin, the creator, or in the allowed users by ID or email
                var hasAccess = isAdmin || 
                               question.Template.CreatorId == userId || 
                               question.Template.AllowedUsers.Any(au => au.UserId == userId) ||
                               (!string.IsNullOrEmpty(userEmail) && question.Template.AllowedUsers.Any(au => au.Email == userEmail));
                
                if (!hasAccess)
                {
                    TempData["Error"] = "You don't have permission to edit this template.";
                    return RedirectToAction("Index", "FormTemplate");
                }
                
                // First, find and delete all answers for this question
                if (question.Answers != null && question.Answers.Any())
                {
                    _context.Answers.RemoveRange(question.Answers);
                    await _context.SaveChangesAsync();
                }
                
                // Then, remove all options for this question
                var options = await _context.QuestionOptions
                    .Where(o => o.QuestionId == id)
                    .ToListAsync();
                
                _context.QuestionOptions.RemoveRange(options);
                await _context.SaveChangesAsync();
                
                // Then, remove the question
                _context.Questions.Remove(question);
                await _context.SaveChangesAsync();
                
                // Reorder remaining questions
                var remainingQuestions = await _context.Questions
                    .Where(q => q.TemplateId == templateId)
                    .OrderBy(q => q.Order)
                    .ToListAsync();
                
                for (int i = 0; i < remainingQuestions.Count; i++)
                {
                    remainingQuestions[i].Order = i + 1;
                }
                
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Question deleted successfully.";
                return RedirectToAction("Index", new { id = templateId });
            }
            catch (Exception ex)
            {
                // Get inner exception message for more detail
                string errorMsg = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMsg += " - " + ex.InnerException.Message;
                }
                
                TempData["Error"] = $"An error occurred: {errorMsg}";
                return RedirectToAction("Index", "FormTemplate");
            }
        }
        
        // POST: /Question/Reorder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reorder(int id, string questionIds)
        {
            try
            {
                // Get the template
                var template = await _context.FormTemplates
                    .Include(t => t.AllowedUsers)
                    .FirstOrDefaultAsync(t => t.Id == id);
                    
                if (template == null)
                {
                    TempData["Error"] = "Template not found.";
                    return RedirectToAction("Index", "FormTemplate");
                }
                
                // Check if user has permission to edit this template
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var isAdmin = User.IsInRole("Admin");
                
                // User has access if they're an admin, the creator, or in the allowed users by ID or email
                var hasAccess = isAdmin || 
                               template.CreatorId == userId || 
                               template.AllowedUsers.Any(au => au.UserId == userId) ||
                               (!string.IsNullOrEmpty(userEmail) && template.AllowedUsers.Any(au => au.Email == userEmail));
                
                if (!hasAccess)
                {
                    TempData["Error"] = "You don't have permission to edit this template.";
                    return RedirectToAction("Index", "FormTemplate");
                }
                
                // Parse the question IDs from the JSON string
                var ids = JsonSerializer.Deserialize<List<int>>(questionIds);
                
                if (ids == null || !ids.Any())
                {
                    TempData["Error"] = "Invalid question order data.";
                    return RedirectToAction("Index", new { id });
                }
                
                // Get all questions for this template
                var questions = await _context.Questions
                    .Where(q => q.TemplateId == id)
                    .ToListAsync();
                
                // Check if all supplied question IDs belong to this template
                var templateQuestionIds = questions.Select(q => q.Id).ToHashSet();
                if (ids.Any(id => !templateQuestionIds.Contains(id)))
                {
                    TempData["Error"] = "Invalid question data. Some questions don't belong to this template.";
                    return RedirectToAction("Index", new { id });
                }
                
                // Update the order of each question
                for (int i = 0; i < ids.Count; i++)
                {
                    var question = questions.FirstOrDefault(q => q.Id == ids[i]);
                    if (question != null)
                    {
                        question.Order = i + 1;
                    }
                }
                
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index", new { id });
            }
        }
    }
} 