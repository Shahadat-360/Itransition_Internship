using FormsApp.Data;
using FormsApp.Models;
using FormsApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FormsApp.Controllers
{
    [Authorize]
    public class FormResponseController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        public FormResponseController(ApplicationDbContext context)
        {
            _context = context;
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
            if (!template.IsPublic)
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                var isAdmin = User.IsInRole("Admin");
                
                var isAllowedById = await _context.AllowedUsers
                    .AnyAsync(au => au.TemplateId == id && au.UserId == currentUserId);
                    
                var isAllowedByEmail = !string.IsNullOrEmpty(userEmail) && await _context.AllowedUsers
                    .AnyAsync(au => au.TemplateId == id && au.Email == userEmail);
                    
                if (!isAdmin && template.CreatorId != currentUserId && !isAllowedById && !isAllowedByEmail)
                {
                    TempData["ErrorMessage"] = "You don't have permission to fill out this form.";
                    return RedirectToAction("Index", "FormTemplate");
                }
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
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var isAdmin = User.IsInRole("Admin");
            
            if (!template.IsPublic)
            {
                var isAllowedById = await _context.AllowedUsers
                    .AnyAsync(au => au.TemplateId == templateId && au.UserId == currentUserId);
                    
                var isAllowedByEmail = !string.IsNullOrEmpty(userEmail) && await _context.AllowedUsers
                    .AnyAsync(au => au.TemplateId == templateId && au.Email == userEmail);
                    
                if (!isAdmin && template.CreatorId != currentUserId && !isAllowedById && !isAllowedByEmail)
                {
                    TempData["ErrorMessage"] = "You don't have permission to fill out this form.";
                    return RedirectToAction("Index", "FormTemplate");
                }
            }
            
            // Create a new form response
            var response = new FormResponse
            {
                TemplateId = templateId,
                RespondentId = currentUserId,
                CreatedAt = DateTime.UtcNow,
                SubmittedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow,
                Answers = new List<Answer>()
            };
            
            // Process the form data
            foreach (var question in template.Questions)
            {
                var questionKey = $"question_{question.Id}";
                string answerText = string.Empty;
                
                // Handle different question types
                switch (question.Type)
                {
                    case QuestionType.SingleLineText:
                    case QuestionType.MultiLineText:
                        if (formData.TryGetValue(questionKey, out var textValue))
                        {
                            answerText = textValue;
                        }
                        break;
                        
                    case QuestionType.MultipleChoice:
                    case QuestionType.Poll:
                        if (formData.TryGetValue(questionKey, out var optionId))
                        {
                            // Check if the value is an ID or text
                            if (int.TryParse(optionId, out int parsedOptionId))
                            {
                                // Find the option by ID and use the ID as answerText
                                answerText = optionId;
                            }
                            else
                            {
                                // Use the text value directly
                                answerText = optionId;
                            }
                        }
                        break;
                        
                    case QuestionType.Integer:
                        if (formData.TryGetValue(questionKey, out var intValue))
                        {
                            answerText = intValue;
                        }
                        break;
                        
                    default:
                        // Skip unsupported question types
                        continue;
                }
                
                // Check if the question is required
                if (question.Required && string.IsNullOrWhiteSpace(answerText))
                {
                    ModelState.AddModelError(string.Empty, $"Question '{question.Text}' is required.");
                    continue;
                }
                
                // Add the answer
                if (!string.IsNullOrWhiteSpace(answerText))
                {
                    var answer = new Answer
                    {
                        QuestionId = question.Id,
                        Text = answerText,
                        CreatedAt = DateTime.UtcNow
                        // Let EF Core handle ResponseId via navigation property
                    };
                    
                    response.Answers.Add(answer);
                }
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
            
            // Save the response and its related answers in one go.
            // EF Core should handle setting the Answer.ResponseId automatically.
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
            
            // For debugging - log answer count
            Console.WriteLine($"Response {id} fetched. Found {response.Answers?.Count ?? 0} answers.");
            
            // Check if the user is allowed to view this response
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            
            if (!isAdmin && response.RespondentId != currentUserId && response.Template.CreatorId != currentUserId)
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
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            
            if (!isAdmin && response.RespondentId != currentUserId)
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
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            
            if (!isAdmin && response.RespondentId != currentUserId)
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
            
            // Update the answers
            foreach (var question in response.Template.Questions)
            {
                var questionKey = $"question_{question.Id}";
                string answerText = string.Empty;
                
                // Handle different question types
                switch (question.Type)
                {
                    case QuestionType.SingleLineText:
                    case QuestionType.MultiLineText:
                        if (formData.TryGetValue(questionKey, out var textValue))
                        {
                            answerText = textValue;
                        }
                        break;
                        
                    case QuestionType.MultipleChoice:
                    case QuestionType.Poll:
                        if (formData.TryGetValue(questionKey, out var optionId))
                        {
                            // Check if the value is an ID or text
                            if (int.TryParse(optionId, out int parsedOptionId))
                            {
                                // Find the option by ID and use the ID as answerText
                                answerText = optionId;
                            }
                            else
                            {
                                // Use the text value directly
                                answerText = optionId;
                            }
                        }
                        break;
                        
                    case QuestionType.Integer:
                        if (formData.TryGetValue(questionKey, out var intValue))
                        {
                            answerText = intValue;
                        }
                        break;
                        
                    default:
                        // Skip unsupported question types
                        continue;
                }
                
                // Check if the question is required
                if (question.Required && string.IsNullOrWhiteSpace(answerText))
                {
                    ModelState.AddModelError(string.Empty, $"Question '{question.Text}' is required.");
                    continue;
                }
                
                // Find or create the answer
                var answer = response.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
                
                if (answer == null)
                {
                    // Create a new answer if it doesn't exist
                    if (!string.IsNullOrWhiteSpace(answerText))
                    {
                        answer = new Answer
                        {
                            QuestionId = question.Id,
                            ResponseId = response.Id,
                            Text = answerText,
                            CreatedAt = DateTime.UtcNow
                        };
                        
                        response.Answers.Add(answer);
                        _context.Answers.Add(answer);
                    }
                }
                else
                {
                    // Update the existing answer
                    if (string.IsNullOrWhiteSpace(answerText))
                    {
                        // Remove the answer if it's empty
                        response.Answers.Remove(answer);
                        _context.Answers.Remove(answer);
                    }
                    else
                    {
                        // Update the answer text
                        answer.Text = answerText;
                        _context.Answers.Update(answer);
                    }
                }
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
            
            // Update the response
            response.LastModifiedAt = DateTime.UtcNow;
            
            // Save
            await _context.SaveChangesAsync();
            
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
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            
            if (!isAdmin && response.RespondentId != currentUserId)
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
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            
            if (!isAdmin && response.RespondentId != currentUserId)
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
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            
            if (!isAdmin && template.CreatorId != currentUserId)
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
            
            Console.WriteLine($"Found {responses.Count} total responses for template {id}");
            Console.WriteLine($"Total answers: {responses.SelectMany(r => r.Answers).Count()}");
            
            // Get all answers for this template for efficient querying
            var allAnswers = await _context.Answers
                .Where(a => a.Response.TemplateId == id)
                .ToListAsync();
                
            Console.WriteLine($"Total answers from direct query: {allAnswers.Count}");
            
            // For each question, log how many answers exist
            foreach (var question in template.Questions)
            {
                var answersForQuestion = allAnswers.Where(a => a.QuestionId == question.Id).ToList();
                Console.WriteLine($"Question {question.Id} ({question.Text}) has {answersForQuestion.Count} answers");
                foreach (var answer in answersForQuestion.Take(5))
                {
                    Console.WriteLine($"  - Answer: {answer.Text}");
                }
            }
            
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
                        QuestionResults = CalculateQuestionResults(q, responses),
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

        // Helper method to calculate question results
        private Dictionary<string, int> CalculateQuestionResults(Question question, List<FormResponse> responses)
        {
            var results = new Dictionary<string, int>();
            
            // Get all answers for this question
            var answers = responses
                .SelectMany(r => r.Answers)
                .Where(a => a.QuestionId == question.Id)
                .ToList();
                
            // For Multiple Choice and Poll questions
            if (question.Type == QuestionType.MultipleChoice || question.Type == QuestionType.Poll)
            {
                // Create mapping of option text to option ID for debugging
                Console.WriteLine($"Processing question {question.Id} with {question.Options.Count} options and {answers.Count} answers");
                foreach (var option in question.Options.OrderBy(o => o.Order))
                {
                    Console.WriteLine($"Option ID: {option.Id}, Text: '{option.Text}'");
                    // Initialize counts to zero
                    results[option.Text] = 0;
                }
                
                // Debug the answers
                foreach (var answer in answers)
                {
                    Console.WriteLine($"Answer for question {question.Id}: '{answer.Text}'");
                }
                
                // Update counts based on answers
                foreach (var answer in answers)
                {
                    if (int.TryParse(answer.Text, out int optionId))
                    {
                        var option = question.Options.FirstOrDefault(o => o.Id == optionId);
                        if (option != null)
                        {
                            Console.WriteLine($"Found match for option ID {optionId} (text: {option.Text})");
                            results[option.Text]++;
                        }
                        else
                        {
                            Console.WriteLine($"Warning: No option found with ID {optionId} for question {question.Id}");
                        }
                    }
                    else
                    {
                        // Check if answer.Text directly matches any option.Text (for older data)
                        var option = question.Options.FirstOrDefault(o => o.Text == answer.Text);
                        if (option != null)
                        {
                            Console.WriteLine($"Found text match for '{answer.Text}'");
                            results[option.Text]++;
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Answer '{answer.Text}' doesn't match any option ID or text");
                        }
                    }
                }
                
                // Log the final counts
                foreach (var result in results)
                {
                    Console.WriteLine($"Final count for '{result.Key}': {result.Value}");
                }
            }
            // For text questions
            else if (question.Type == QuestionType.SingleLineText || question.Type == QuestionType.MultiLineText)
            {
                // Group by unique answers
                var answerGroups = answers
                    .GroupBy(a => a.Text)
                    .Select(g => new { Text = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .Take(10); // Take top 10 most common answers
                    
                foreach (var group in answerGroups)
                {
                    results.Add(group.Text, group.Count);
                }
            }
            
            return results;
        }

        // Temporary fix action to diagnose and fix response issues
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> FixResponse(int id)
        {
            // Find the response
            var response = await _context.FormResponses
                .Include(r => r.Template)
                    .ThenInclude(t => t.Questions)
                .Include(r => r.Answers)
                .FirstOrDefaultAsync(r => r.Id == id);
                
            if (response == null)
            {
                TempData["ErrorMessage"] = "Response not found.";
                return RedirectToAction("Index");
            }
            
            // Check if we have answers
            if (response.Answers == null || !response.Answers.Any())
            {
                TempData["WarningMessage"] = "This response has no answers. Checking for disconnected answers...";
                
                // Look for any disconnected answers
                var disconnectedAnswers = await _context.Answers
                    .Where(a => a.ResponseId == id)
                    .ToListAsync();
                    
                if (disconnectedAnswers.Any())
                {
                    TempData["SuccessMessage"] = $"Found {disconnectedAnswers.Count} disconnected answers and reconnected them.";
                    
                    // Add them to the response
                    foreach (var answer in disconnectedAnswers)
                    {
                        if (response.Answers.All(a => a.Id != answer.Id))
                        {
                            response.Answers.Add(answer);
                        }
                    }
                    
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // If no disconnected answers, show the questions from the template
                    var questions = response.Template.Questions;
                    TempData["InfoMessage"] = $"No disconnected answers found. Template has {questions.Count()} questions.";
                }
            }
            else
            {
                TempData["InfoMessage"] = $"Response already has {response.Answers.Count} answers.";
            }
            
            return RedirectToAction("Details", new { id });
        }

        // GET: /FormResponse/GetPollResults/{questionId}
        [HttpGet]
        public async Task<IActionResult> GetPollResults(int questionId)
        {
            try
            {
                // Log the request for debugging
                Console.WriteLine($"GetPollResults: Fetching poll results for questionId={questionId}");
                
                if (questionId < 0)
                {
                    Console.WriteLine($"GetPollResults: Invalid questionId {questionId}");
                    return Json(new { 
                        success = false, 
                        message = "Invalid question ID",
                        errorDetails = $"Question ID must be positive, received: {questionId}" 
                    });
                }
                
                // Get the question with eager loading
                var question = await _context.Questions
                    .Include(q => q.Options)
                    .FirstOrDefaultAsync(q => q.Id == questionId);

                if (question == null)
                {
                    Console.WriteLine($"GetPollResults: Question with ID {questionId} not found");
                    
                    // As a fallback, check if the question exists without options
                    var questionExists = await _context.Questions.AnyAsync(q => q.Id == questionId);
                    
                    if (questionExists)
                    {
                        Console.WriteLine($"GetPollResults: Question exists but has no options");
                        return Json(new { 
                            success = false, 
                            message = "Question has no options",
                            errorDetails = "The question was found but has no associated options"
                        });
                    }
                    
                    return Json(new { 
                        success = false, 
                        message = "Question not found",
                        errorDetails = $"No question with ID {questionId} exists in the database" 
                    });
                }
                
                // Check if question has options
                if (question.Options == null || !question.Options.Any())
                {
                    Console.WriteLine($"GetPollResults: Question {questionId} has no options");
                    return Json(new { 
                        success = false, 
                        message = "This poll has no options",
                        errorDetails = "The poll question has no options configured" 
                    });
                }
                
                // Get all answers for this question
                var answers = await _context.Answers
                    .Where(a => a.QuestionId == questionId)
                    .ToListAsync();
                    
                Console.WriteLine($"GetPollResults: Found {answers.Count} answers for question {questionId}");
                
                // Initialize results dictionary
                var resultsList = new List<object>();
                int totalVotes = answers.Count;
                
                // Calculate vote counts for each option
                foreach (var option in question.Options.OrderBy(o => o.Order))
                {
                    // Count how many times this option was selected
                    int votes = answers.Count(a => a.Text == option.Id.ToString());
                    
                    resultsList.Add(new 
                    { 
                        optionId = option.Id.ToString(),
                        optionText = option.Text,
                        votes = votes,
                        percentage = totalVotes > 0 ? Math.Round((double)votes / totalVotes * 100) : 0
                    });
                }
                
                // If no responses yet, still return all options with 0 votes
                if (totalVotes == 0 && question.Options.Any())
                {
                    // Return 0 votes for every option to initialize the UI
                    totalVotes = 1; // Just to avoid division by zero when calculating percentages
                }
                
                // Create a dictionary with option IDs as keys for easy lookup
                var resultsDict = resultsList.ToDictionary(
                    r => ((dynamic)r).optionId,
                    r => r
                );
                
                Console.WriteLine($"GetPollResults: Successfully processed results for question {questionId}");
                
                return Json(new 
                { 
                    success = true,
                    questionId = questionId,
                    totalVotes = totalVotes,
                    results = resultsDict
                });
            }
            catch (Exception ex)
            {
                // Log the error with more details
                Console.WriteLine($"Error in GetPollResults for questionId={questionId}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Return a more detailed error message for debugging
                return Json(new { 
                    success = false, 
                    message = "An error occurred while retrieving poll results", 
                    errorDetails = $"{ex.Message} - {ex.GetType().Name}" 
                });
            }
        }
    }
} 