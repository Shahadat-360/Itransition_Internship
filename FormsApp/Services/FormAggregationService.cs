using FormsApp.Data;
using FormsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FormsApp.Services
{
    public class FormAggregationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FormAggregationService> _logger;

        public FormAggregationService(ApplicationDbContext context, ILogger<FormAggregationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<TemplateAggregateResult>> GetAggregatedResultsForUserAsync(string userId)
        {
            // Get all templates created by the user
            var templates = await _context.FormTemplates
                .Include(t => t.Questions)
                .Where(t => t.CreatorId == userId)
                .ToListAsync();
            
            var results = new List<TemplateAggregateResult>();
            
            foreach (var template in templates)
            {
                var templateResult = new TemplateAggregateResult
                {
                    TemplateId = template.Id,
                    Title = template.Title,
                    Description = template.Description,
                    CreatedAt = template.CreatedAt,
                    ResponseCount = await _context.FormResponses.CountAsync(r => r.TemplateId == template.Id),
                    Questions = await GetAggregatedQuestionsAsync(template.Id, template.Questions.ToList())
                };
                
                results.Add(templateResult);
            }
            
            return results;
        }

        public async Task<TemplateAggregateResult?> GetAggregatedResultsByTemplateIdAsync(int templateId, string userId)
        {
            // Get the template and verify ownership
            var template = await _context.FormTemplates
                .Include(t => t.Questions)
                .FirstOrDefaultAsync(t => t.Id == templateId && t.CreatorId == userId);
            
            if (template == null)
                return null;
            
            var result = new TemplateAggregateResult
            {
                TemplateId = template.Id,
                Title = template.Title,
                Description = template.Description,
                CreatedAt = template.CreatedAt,
                ResponseCount = await _context.FormResponses.CountAsync(r => r.TemplateId == template.Id),
                Questions = await GetAggregatedQuestionsAsync(template.Id, template.Questions.ToList())
            };
            
            return result;
        }

        private async Task<List<QuestionAggregateResult>> GetAggregatedQuestionsAsync(int templateId, List<Question> questions)
        {
            var result = new List<QuestionAggregateResult>();
            
            foreach (var question in questions)
            {
                // Get all answers for this question across responses
                var answers = await _context.Answers
                    .Include(a => a.Response)
                    .Where(a => a.QuestionId == question.Id && a.Response.TemplateId == templateId)
                    .ToListAsync();
                
                var questionResult = new QuestionAggregateResult
                {
                    QuestionId = question.Id,
                    Text = question.Text,
                    Type = question.Type,
                    ResponseCount = answers.Count,
                    AggregatedData = CalculateAggregation(question.Type, answers)
                };
                
                result.Add(questionResult);
            }
            
            return result;
        }

        private Dictionary<string, object> CalculateAggregation(QuestionType type, List<Answer> answers)
        {
            var result = new Dictionary<string, object>();
            
            switch (type)
            {
                case QuestionType.Integer:
                    // Calculate numeric aggregates
                    var intValues = answers.Where(a => a.IntValue.HasValue).Select(a => a.IntValue.Value).ToList();
                    if (intValues.Any())
                    {
                        result["count"] = intValues.Count;
                        result["average"] = intValues.Average();
                        result["min"] = intValues.Min();
                        result["max"] = intValues.Max();
                        result["sum"] = intValues.Sum();
                    }
                    break;
                    
                case QuestionType.MultipleChoice:
                case QuestionType.Poll:
                    // Count occurrences of each option
                    var optionGroups = answers.GroupBy(a => a.TextValue)
                        .Select(g => new { Option = g.Key, Count = g.Count() })
                        .OrderByDescending(item => item.Count)
                        .ToList();
                    
                    // Calculate total responses for percentage calculation
                    int totalResponses = answers.Count;
                    
                    // Create dictionary with option -> percentage
                    var options = optionGroups.ToDictionary(
                        item => item.Option ?? "No Answer", 
                        item => totalResponses > 0 
                            ? Math.Round((double)item.Count / totalResponses * 100, 1) 
                            : 0.0
                    );
                    
                    result["options"] = options;
                    result["totalResponses"] = totalResponses;
                    break;
                    
                case QuestionType.SingleLineText:
                case QuestionType.MultiLineText:
                    // Get top 5 most common responses
                    var textValues = answers.Where(a => !string.IsNullOrEmpty(a.TextValue))
                        .GroupBy(a => a.TextValue)
                        .Select(g => new { Text = g.Key, Count = g.Count() })
                        .OrderByDescending(item => item.Count)
                        .Take(5)
                        .ToDictionary(item => item.Text ?? "No Answer", item => item.Count);
                    
                    result["mostCommon"] = textValues;
                    
                    // Length statistics removed as requested
                    break;
                
                default:
                    result["message"] = "No aggregation available for this question type";
                    break;
            }
            
            return result;
        }
    }

    // Classes to represent the aggregated results
    public class TemplateAggregateResult
    {
        public int TemplateId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ResponseCount { get; set; }
        public List<QuestionAggregateResult> Questions { get; set; } = new List<QuestionAggregateResult>();
    }

    public class QuestionAggregateResult
    {
        public int QuestionId { get; set; }
        public string Text { get; set; } = string.Empty;
        public QuestionType Type { get; set; }
        public int ResponseCount { get; set; }
        public Dictionary<string, object> AggregatedData { get; set; } = new Dictionary<string, object>();
    }
} 