using FormsApp.Data;
using FormsApp.Models;
using FormsApp.ViewModels;
using FormsApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FormsApp.Controllers
{
    [Authorize]
    public class FormResponseController(ApplicationDbContext context, FormResponseService responseService) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly FormResponseService _responseService = responseService;

        // Helper method to check if a user has permission to access a form template
        private async Task<bool> HasAccessToTemplate(FormTemplate template, string userId, string userEmail, bool isAdmin)
        {
            if (template.IsPublic)
                return true;
                
            if (isAdmin || template.CreatorId == userId)
                return true;
                
            var isAllowedById = await _context.AllowedUsers
                .AnyAsync(au => au.TemplateId == template.Id && au.UserId == userId);
                
            var isAllowedByEmail = !string.IsNullOrEmpty(userEmail) && await _context.AllowedUsers
                .AnyAsync(au => au.TemplateId == template.Id && au.Email == userEmail);
                
            return isAllowedById || isAllowedByEmail;
        }
        
        // Helper method to check if a user has permission to access a response
        private bool HasAccessToResponse(FormResponse response, string userId, bool isAdmin)
        {
            return isAdmin || response.RespondentId == userId || response.Template.CreatorId == userId;
        }

        // GET: /FormResponse
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var responses = await _context.FormResponses
                .Where(r => r.RespondentId == userId)
                .Include(r => r.Template)
                .OrderByDescending(r => r.SubmittedAt)
                .Select(r => new FormResponseViewModel
                {
                    Id = r.Id,
                    TemplateId = r.TemplateId,
                    TemplateName = r.Template.Title,
                    CreatorName = r.Template.Creator.UserName,
                    SubmittedAt = r.SubmittedAt
                })
                .ToListAsync();
                
            return View(responses);
        }
        
        // GET: /FormResponse/Create/{templateId}
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Create(int id)
        {
            // Find the form template
            var template = await _context.FormTemplates
                .Include(t => t.Questions)
                    .ThenInclude(q => q.Options.OrderBy(o => o.Order))
                .FirstOrDefaultAsync(t => t.Id == id);
                
            if (template == null)
            {
                TempData["ErrorMessage"] = "Form template not found.";
                return RedirectToAction("Index", "FormTemplate");
            }
            
            // Check if the user is allowed to fill out this form
            if (!await HasAccessToTemplate(template, User.FindFirstValue(ClaimTypes.NameIdentifier), User.FindFirstValue(ClaimTypes.Email), User.IsInRole("Admin")))
            {
                TempData["ErrorMessage"] = "You don't have permission to fill out this form.";
                return RedirectToAction("Index", "FormTemplate");
            }
            
            // Set up ViewData for the view
            ViewData["Template"] = template;
            ViewData["Questions"] = template.Questions.OrderBy(q => q.Order).ToList();
            
            return View();
        }
        
        // POST: /FormResponse/Create/{templateId}
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int templateId, Dictionary<string, string> formData)
        {
            // Find the form template
            var template = await _context.FormTemplates
                .Include(t => t.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(t => t.Id == templateId);
                
            if (template == null)
            {
                TempData["ErrorMessage"] = "Form template not found.";
                return RedirectToAction("Index", "FormTemplate");
            }
            
            // Check if the user is allowed to fill out this form
            if (!await HasAccessToTemplate(template, User.FindFirstValue(ClaimTypes.NameIdentifier), User.FindFirstValue(ClaimTypes.Email), User.IsInRole("Admin")))
            {
                TempData["ErrorMessage"] = "You don't have permission to fill out this form.";
                return RedirectToAction("Index", "FormTemplate");
            }
            
            // Create a new form response
            var response = new FormResponse
            {
                TemplateId = templateId,
                RespondentId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                CreatedAt = DateTime.UtcNow,
                SubmittedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow,
                Answers = new List<Answer>()
            };
            
            // Process form data using the service
            var validationErrors = new List<string>();
            var answers = _responseService.ProcessFormData(template, formData, validationErrors);
            
            // Add validation errors to ModelState
            foreach (var error in validationErrors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            
            // Validate
            if (!ModelState.IsValid)
            {
                var model = new FormResponseViewModel
                {
                    TemplateId = template.Id,
                    TemplateTitle = template.Title,
                    Questions = template.Questions.Select(q => new QuestionViewModel
                    {
                        Id = q.Id,
                        Text = q.Text,
                        Type = q.Type,
                        Required = q.Required,
                        Order = q.Order,
                        Options = q.Options.Select(o => new OptionViewModel
                        {
                            Id = o.Id,
                            Text = o.Text,
                            Order = o.Order
                        }).ToList()
                    }).OrderBy(q => q.Order).ToList()
                };
                
                return View(model);
            }
            
            // Add answers to the response
            foreach (var answer in answers)
            {
                response.Answers.Add(answer);
            }
            
            // Save the response and its related answers
            _context.FormResponses.Add(response);
            await _context.SaveChangesAsync(); 
            
            TempData["SuccessMessage"] = "Your response has been submitted successfully.";
            return RedirectToAction(nameof(Details), new { id = response.Id });
        }
        
        // GET: /FormResponse/Details/{id}
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            // Find the response with explicit loading of all related data
            var response = await _context.FormResponses
                .Include(r => r.Template)
                    .ThenInclude(t => t.Creator)
                .Include(r => r.Respondent)
                .Include(r => r.Answers)
                    .ThenInclude(a => a.Question)
                        .ThenInclude(q => q.Options)
                .AsNoTracking() // Using AsNoTracking for read-only operation
                .FirstOrDefaultAsync(r => r.Id == id);
                
            if (response == null)
            {
                TempData["ErrorMessage"] = "Form response not found.";
                return RedirectToAction("Index");
            }
            
            // Check if the user is allowed to view this response
            if (!HasAccessToResponse(response, User.FindFirstValue(ClaimTypes.NameIdentifier), User.IsInRole("Admin")))
            {
                TempData["ErrorMessage"] = "You don't have permission to view this response.";
                return RedirectToAction("Index");
            }
            
            // Create the view model
            var model = new FormResponseViewModel
            {
                Id = response.Id,
                TemplateId = response.TemplateId,
                TemplateTitle = response.Template.Title,
                TemplateName = response.Template.Title,
                RespondentName = response.Respondent?.UserName ?? "Unknown User",
                RespondentEmail = response.Respondent?.Email ?? "No email provided",
                CreatedAt = response.CreatedAt,
                SubmittedAt = response.SubmittedAt, // Use the SubmittedAt from the model
                LastModifiedAt = response.LastModifiedAt,
                Version = Convert.ToBase64String(response.Version),
                CreatorName = response.Template.Creator?.UserName ?? "Unknown",
                Answers = response.Answers?.Select(a => 
                { 
                    // Ensure question is not null before accessing properties
                    var questionTitle = a.Question?.Text ?? "Unknown Question";
                    var questionType = a.Question?.Type ?? QuestionType.SingleLineText; // Default type if null
                    
                    // For multiple choice questions, try to get the option text
                    var displayText = a.Text;
                    if (a.Question != null && (a.Question.Type == QuestionType.MultipleChoice || a.Question.Type == QuestionType.Poll) 
                        && int.TryParse(a.Text, out int optionId))
                    {
                        var option = a.Question.Options?.FirstOrDefault(o => o.Id == optionId);
                        if (option != null)
                        {
                            displayText = option.Text;
                        }
                    }
                    
                    return new AnswerViewModel
                    {
                        Id = a.Id,
                        QuestionId = a.QuestionId,
                        QuestionTitle = questionTitle,
                        QuestionType = questionType,
                        Text = displayText,
                        TextValue = displayText,
                        CreatedAt = a.CreatedAt
                    };
                }).ToList() ?? new List<AnswerViewModel>() // Ensure list is never null
            };
            
            return View(model);
        }
        
        // GET: /FormResponse/Edit/{id}
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            // Find the response
            var response = await _context.FormResponses
                .Include(r => r.Template)
                    .ThenInclude(t => t.Questions)
                        .ThenInclude(q => q.Options)
                .Include(r => r.Answers)
                .FirstOrDefaultAsync(r => r.Id == id);
                
            if (response == null)
            {
                TempData["ErrorMessage"] = "Form response not found.";
                return RedirectToAction("Index");
            }
            
            // Check if the user is allowed to edit this response
            if (!HasAccessToResponse(response, User.FindFirstValue(ClaimTypes.NameIdentifier), User.IsInRole("Admin")))
            {
                TempData["ErrorMessage"] = "You don't have permission to edit this response.";
                return RedirectToAction("Index");
            }
            
            // Set up the ViewData for the edit view
            ViewData["Template"] = response.Template;
            ViewData["Questions"] = response.Template.Questions.OrderBy(q => q.Order).ToList();
            ViewData["ResponseId"] = response.Id;
            ViewData["Version"] = Convert.ToBase64String(response.Version);
            
            // Create a dictionary of question ID to answer
            var answersDictionary = response.Answers.ToDictionary(a => a.QuestionId);
            ViewData["Answers"] = answersDictionary;
            
            return View();
        }
        
        // POST: /FormResponse/Edit/{id}
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string version, Dictionary<string, string> formData)
        {
            // Find the response
            var response = await _context.FormResponses
                .Include(r => r.Template)
                    .ThenInclude(t => t.Questions)
                        .ThenInclude(q => q.Options)
                .Include(r => r.Answers)
                .FirstOrDefaultAsync(r => r.Id == id);
                
            if (response == null)
            {
                TempData["ErrorMessage"] = "Form response not found.";
                return RedirectToAction("Index");
            }
            
            // Check if the user is allowed to edit this response
            if (!HasAccessToResponse(response, User.FindFirstValue(ClaimTypes.NameIdentifier), User.IsInRole("Admin")))
            {
                TempData["ErrorMessage"] = "You don't have permission to edit this response.";
                return RedirectToAction("Index");
            }
            
            // Check for concurrency conflicts
            var currentVersion = Convert.ToBase64String(response.Version);
            if (currentVersion != version)
            {
                TempData["WarningMessage"] = "The response was updated by someone else. Your changes were still saved, but please review the response.";
            }
            
            // Process form data using the service
            var validationErrors = new List<string>();
            await _responseService.UpdateResponseAnswers(response, formData, validationErrors);
            
            // Add validation errors to ModelState
            foreach (var error in validationErrors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            
            // Validate
            if (!ModelState.IsValid)
            {
                var model = new FormResponseViewModel
                {
                    Id = response.Id,
                    TemplateId = response.TemplateId,
                    TemplateTitle = response.Template.Title,
                    CreatedAt = response.CreatedAt,
                    LastModifiedAt = response.LastModifiedAt,
                    Version = Convert.ToBase64String(response.Version),
                    Questions = response.Template.Questions.Select(q => new QuestionViewModel
                    {
                        Id = q.Id,
                        Text = q.Text,
                        Type = q.Type,
                        Required = q.Required,
                        Order = q.Order,
                        Options = q.Options.Select(o => new OptionViewModel
                        {
                            Id = o.Id,
                            Text = o.Text,
                            Order = o.Order
                        }).ToList(),
                        Answer = response.Answers
                            .Where(a => a.QuestionId == q.Id)
                            .Select(a => new AnswerViewModel
                            {
                                Id = a.Id,
                                Text = a.Text,
                                CreatedAt = a.CreatedAt
                            })
                            .FirstOrDefault()
                    }).OrderBy(q => q.Order).ToList()
                };
                
                return View(model);
            }
            
            TempData["SuccessMessage"] = "Your response has been updated successfully.";
            return RedirectToAction(nameof(Details), new { id = response.Id });
        }
        
        // GET: /FormResponse/Delete/{id}
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            // Find the response
            var response = await _context.FormResponses
                .Include(r => r.Template)
                .FirstOrDefaultAsync(r => r.Id == id);
                
            if (response == null)
            {
                TempData["ErrorMessage"] = "Form response not found.";
                return RedirectToAction("Index");
            }
            
            // Check if the user is allowed to delete this response
            if (!HasAccessToResponse(response, User.FindFirstValue(ClaimTypes.NameIdentifier), User.IsInRole("Admin")))
            {
                TempData["ErrorMessage"] = "You don't have permission to delete this response.";
                return RedirectToAction("Index");
            }
            
            // Create the view model
            var model = new FormResponseViewModel
            {
                Id = response.Id,
                TemplateId = response.TemplateId,
                TemplateTitle = response.Template.Title,
                CreatedAt = response.CreatedAt,
                LastModifiedAt = response.LastModifiedAt
            };
            
            return View(model);
        }
        
        // POST: /FormResponse/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Find the response
            var response = await _context.FormResponses
                .Include(r => r.Answers)
                .FirstOrDefaultAsync(r => r.Id == id);
                
            if (response == null)
            {
                TempData["ErrorMessage"] = "Form response not found.";
                return RedirectToAction("Index");
            }
            
            // Check if the user is allowed to delete this response
            if (!HasAccessToResponse(response, User.FindFirstValue(ClaimTypes.NameIdentifier), User.IsInRole("Admin")))
            {
                TempData["ErrorMessage"] = "You don't have permission to delete this response.";
                return RedirectToAction("Index");
            }
            
            // Delete
            _context.Answers.RemoveRange(response.Answers);
            _context.FormResponses.Remove(response);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Your response has been deleted successfully.";
            return RedirectToAction("Index");
        }
        
        // POST: /FormResponse/DeleteSelected
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelected(List<int> selectedResponses)
        {
            if (selectedResponses == null || !selectedResponses.Any())
            {
                TempData["ErrorMessage"] = "No responses selected for deletion.";
                return RedirectToAction("Index");
            }
            
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            int deletedCount = 0;
            
            foreach (var responseId in selectedResponses)
            {
                // Find the response
                var response = await _context.FormResponses
                    .Include(r => r.Answers)
                    .FirstOrDefaultAsync(r => r.Id == responseId);
                    
                if (response == null) continue;
                
                // Check if the user is allowed to delete this response
                if (!HasAccessToResponse(response, currentUserId, isAdmin)) continue;
                
                // Delete
                _context.Answers.RemoveRange(response.Answers);
                _context.FormResponses.Remove(response);
                deletedCount++;
            }
            
            if (deletedCount > 0)
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Successfully deleted {deletedCount} response(s).";
            }
            else
            {
                TempData["WarningMessage"] = "No responses were deleted. You may not have permission to delete the selected responses.";
            }
            
            return RedirectToAction("Index");
        }
        
        // GET: /FormResponse/ResultsForTemplate/{id}
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ResultsForTemplate(int id)
        {
            // Find the template with eager loading of all required entities
            var template = await _context.FormTemplates
                .Include(t => t.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(t => t.Id == id);
                
            if (template == null)
            {
                TempData["ErrorMessage"] = "Form template not found.";
                return RedirectToAction("Index", "FormTemplate");
            }
            
            // Check if the user is allowed to view results
            if (!await HasAccessToTemplate(template, User.FindFirstValue(ClaimTypes.NameIdentifier), User.FindFirstValue(ClaimTypes.Email), User.IsInRole("Admin")))
            {
                TempData["ErrorMessage"] = "You don't have permission to view results for this template.";
                return RedirectToAction("Index", "FormTemplate");
            }
            
            // Get all responses for this template with eager loading
            var responses = await _context.FormResponses
                .Include(r => r.Answers)
                .Include(r => r.Respondent)
                .Where(r => r.TemplateId == id)
                .ToListAsync();
            
            // Get all answers for this template for efficient querying
            var allAnswers = await _context.Answers
                .Where(a => a.Response.TemplateId == id)
                .ToListAsync();
            
            // Create the view model
            var model = new FormAggregationViewModel
            {
                TemplateId = template.Id,
                TemplateTitle = template.Title,
                TotalResponses = responses.Count,
                Questions = template.Questions
                    .Where(q => q.ShowInResults)
                    .Select(q => new QuestionAggregationViewModel
                    {
                        Id = q.Id,
                        Text = q.Text,
                        Type = q.Type,
                        Order = q.Order,
                        QuestionResults = _responseService.CalculateQuestionResults(q, responses),
                        Options = q.Options.Select(o => new OptionViewModel
                        {
                            Id = o.Id,
                            Text = o.Text,
                            Order = o.Order
                        }).ToList()
                    })
                    .OrderBy(q => q.Order)
                    .ToList()
            };
            
            // Add specialized question models for different question types
            // Text Questions (Short Answer and Long Answer)
            model.TextQuestions = template.Questions
                .Where(q => (q.Type == QuestionType.SingleLineText || q.Type == QuestionType.MultiLineText) && q.ShowInResults)
                .Select(q => new TextQuestionViewModel
                {
                    QuestionId = q.Id,
                    QuestionTitle = q.Text,
                    TextAnswers = responses
                        .SelectMany(r => r.Answers)
                        .Where(a => a.QuestionId == q.Id && !string.IsNullOrEmpty(a.Text))
                        .Select(a => new TextAnswerViewModel
                        {
                            Text = a.Text,
                            SubmittedAt = a.CreatedAt,
                            ResponseId = a.ResponseId,
                            RespondentName = responses.FirstOrDefault(r => r.Id == a.ResponseId)?.Respondent?.UserName,
                            RespondentEmail = responses.FirstOrDefault(r => r.Id == a.ResponseId)?.Respondent?.Email
                        })
                        .OrderByDescending(a => a.SubmittedAt)
                        .ToList()
                })
                .ToList();
            
            // Numeric Questions
            model.NumericQuestions = template.Questions
                .Where(q => q.Type == QuestionType.Integer && q.ShowInResults)
                .Select(q => 
                {
                    var answers = responses
                        .SelectMany(r => r.Answers)
                        .Where(a => a.QuestionId == q.Id && !string.IsNullOrEmpty(a.Text))
                        .Select(a => int.TryParse(a.Text, out int val) ? val : 0)
                        .ToList();
                        
                    return new NumericQuestionViewModel
                    {
                        QuestionId = q.Id,
                        QuestionTitle = q.Text,
                        Average = answers.Any() ? answers.Average() : 0,
                        Min = answers.Any() ? answers.Min() : 0,
                        Max = answers.Any() ? answers.Max() : 0
                    };
                })
                .ToList();
            
            // For the response timeline
            model.ResponsesPerDay = responses
                .GroupBy(r => r.CreatedAt.Date)
                .Select(g => new DailyResponseCount 
                { 
                    Date = g.Key.ToString("MM/dd/yyyy"),
                    Count = g.Count() 
                })
                .OrderBy(d => DateTime.Parse(d.Date))
                .ToList();
                
            // Add the list of individual responses with respondent info
            model.ResponsesList = responses
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ResponseSummaryViewModel
                {
                    Id = r.Id,
                    RespondentId = r.RespondentId,
                    RespondentName = r.Respondent?.UserName ?? "Unknown User",
                    RespondentEmail = r.Respondent?.Email ?? "Email not available",
                    SubmittedAt = r.SubmittedAt,
                    LastModifiedAt = r.LastModifiedAt,
                    ResponseNumber = responses
                        .Where(x => x.RespondentId == r.RespondentId)
                        .OrderBy(x => x.CreatedAt)
                        .ToList()
                        .FindIndex(x => x.Id == r.Id) + 1
                })
                .ToList();
            
            return View(model);
        }

        // GET: /FormResponse/GetPollResults/{questionId}
        [HttpGet]
        public async Task<IActionResult> GetPollResults(int questionId, int templateId)
        {
            try
            {
                // Use the service to get poll results
                var results = await _responseService.GetPollResults(questionId);
                return Json(results);
            }
            catch (ArgumentException ex)
            {
                return Json(new { 
                    success = false, 
                    message = "Question not found",
                    errorDetails = ex.Message 
                });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { 
                    success = false, 
                    message = "Invalid poll configuration",
                    errorDetails = ex.Message 
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = "An error occurred while retrieving poll results", 
                    errorDetails = $"{ex.Message} - {ex.GetType().Name}" 
                });
            }
        }
    }
} 