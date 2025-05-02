using FormsApp.Data;
using FormsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FormsApp.Services
{
    public class FormResponseService
    {
        private readonly ApplicationDbContext _context;
        
        public FormResponseService(ApplicationDbContext context)
        {
            _context = context;
        }
        
        // Process form data and return answers
        public List<Answer> ProcessFormData(FormTemplate template, Dictionary<string, string> formData, List<string> validationErrors)
        {
            var answers = new List<Answer>();
            
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
                            answerText = textValue?.Trim() ?? string.Empty;
                        }
                        break;
                        
                    case QuestionType.MultipleChoice:
                    case QuestionType.Poll:
                        if (formData.TryGetValue(questionKey, out var optionId))
                        {
                            // Store the option ID as answer
                            answerText = optionId;
                        }
                        break;
                        
                    case QuestionType.Integer:
                        if (formData.TryGetValue(questionKey, out var intValue))
                        {
                            // Validate that this is actually an integer
                            if (!string.IsNullOrWhiteSpace(intValue) && !int.TryParse(intValue, out _))
                            {
                                validationErrors.Add($"'{question.Text}' must be a valid number.");
                            }
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
                    validationErrors.Add($"Question '{question.Text}' is required.");
                    continue;
                }
                
                // Add the answer if not empty
                if (!string.IsNullOrWhiteSpace(answerText))
                {
                    var answer = new Answer
                    {
                        QuestionId = question.Id,
                        Text = answerText,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    answers.Add(answer);
                }
            }
            
            return answers;
        }
        
        // Update existing response with new form data
        public async Task UpdateResponseAnswers(FormResponse response, Dictionary<string, string> formData, List<string> validationErrors)
        {
            var answersByQuestionId = response.Answers.ToDictionary(a => a.QuestionId);
            
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
                            answerText = textValue?.Trim() ?? string.Empty;
                        }
                        break;
                        
                    case QuestionType.MultipleChoice:
                    case QuestionType.Poll:
                        if (formData.TryGetValue(questionKey, out var optionId))
                        {
                            answerText = optionId;
                        }
                        break;
                        
                    case QuestionType.Integer:
                        if (formData.TryGetValue(questionKey, out var intValue))
                        {
                            // Validate that this is actually an integer
                            if (!string.IsNullOrWhiteSpace(intValue) && !int.TryParse(intValue, out _))
                            {
                                validationErrors.Add($"'{question.Text}' must be a valid number.");
                            }
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
                    validationErrors.Add($"Question '{question.Text}' is required.");
                    continue;
                }
                
                // Try to find existing answer
                if (answersByQuestionId.TryGetValue(question.Id, out var existingAnswer))
                {
                    if (string.IsNullOrWhiteSpace(answerText))
                    {
                        // Remove the answer if it's empty now
                        response.Answers.Remove(existingAnswer);
                        _context.Answers.Remove(existingAnswer);
                    }
                    else
                    {
                        // Update the existing answer
                        existingAnswer.Text = answerText;
                        _context.Answers.Update(existingAnswer);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(answerText))
                {
                    // Create a new answer
                    var answer = new Answer
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
            
            if (validationErrors.Count == 0)
            {
                // Update the response timestamp
                response.LastModifiedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        
        // Calculate question results for the results view
        public Dictionary<string, int> CalculateQuestionResults(Question question, List<FormResponse> responses)
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
                // Initialize counts to zero for all options
                foreach (var option in question.Options.OrderBy(o => o.Order))
                {
                    results[option.Text] = 0;
                }
                
                // Update counts based on answers
                foreach (var answer in answers)
                {
                    if (int.TryParse(answer.Text, out int optionId))
                    {
                        var option = question.Options.FirstOrDefault(o => o.Id == optionId);
                        if (option != null)
                        {
                            results[option.Text]++;
                        }
                    }
                    else
                    {
                        // Check if answer.Text directly matches any option.Text (for older data)
                        var option = question.Options.FirstOrDefault(o => o.Text == answer.Text);
                        if (option != null)
                        {
                            results[option.Text]++;
                        }
                    }
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
        
        // Get results for a poll question (for AJAX requests)
        public async Task<Dictionary<string, object>> GetPollResults(int questionId)
        {
            // Get the question with eager loading
            var question = await _context.Questions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null)
            {
                throw new ArgumentException($"Question with ID {questionId} not found");
            }
            
            // Check if question has options
            if (question.Options == null || !question.Options.Any())
            {
                throw new InvalidOperationException("This poll has no options configured");
            }
            
            // Get all answers for this question
            var answers = await _context.Answers
                .Where(a => a.QuestionId == questionId)
                .ToListAsync();
                
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
                r => ((dynamic)r).optionId.ToString(),
                r => r
            );
            
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["questionId"] = questionId,
                ["totalVotes"] = totalVotes,
                ["results"] = resultsDict
            };
        }
    }
} 