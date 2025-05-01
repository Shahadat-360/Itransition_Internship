using System.ComponentModel.DataAnnotations;
using FormsApp.Models;

namespace FormsApp.ViewModels
{
    public class QuestionViewModel
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Text { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [Required]
        public QuestionType Type { get; set; }
        
        public int Order { get; set; }
        
        public bool Required { get; set; } = false;
        
        public bool ShowInResults { get; set; } = true;
        
        // Add TemplateId property needed by controllers
        public int TemplateId { get; set; }
        
        public List<OptionViewModel> Options { get; set; } = new List<OptionViewModel>();
        
        public AnswerViewModel? Answer { get; set; }
    }
    
    public class OptionViewModel
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public int Order { get; set; }
        public int QuestionId { get; set; }
    }

    public class FormResponseViewModel
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public string TemplateTitle { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public string FormTemplateDescription { get; set; } = string.Empty;
        public string FormTemplateName { get; set; } = string.Empty;
        public int FormTemplateId { get; set; }
        public string CreatorName { get; set; } = string.Empty;
        public string RespondentName { get; set; } = string.Empty;
        public string RespondentEmail { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastModifiedAt { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string? Version { get; set; }
        public List<QuestionViewModel> Questions { get; set; } = new List<QuestionViewModel>();
        public List<AnswerViewModel> Answers { get; set; } = new List<AnswerViewModel>();
    }
    
    public class AnswerViewModel
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        
        private string _text = string.Empty;
        public string Text 
        { 
            get => _text;
            set {
                _text = value;
                _responseText = value;
            }
        }
        
        private string _responseText = string.Empty;
        public string ResponseText 
        { 
            get => _responseText;
            set {
                _responseText = value;
                _text = value;
            }
        }
        
        // Add a property to store the question's title for display
        public string QuestionTitle { get; set; } = string.Empty;
        
        // Add a property to store the question's type for display
        public QuestionType QuestionType { get; set; }
        
        // Support for textual and numeric responses
        public string TextValue { get; set; } = string.Empty;
        public int? IntValue { get; set; }
        public bool? BoolValue { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }
    
    public class FormAggregationViewModel
    {
        public int TemplateId { get; set; }
        public string TemplateTitle { get; set; } = string.Empty;
        public int TotalResponses { get; set; }
        public List<QuestionAggregationViewModel> Questions { get; set; } = new List<QuestionAggregationViewModel>();
        
        // Add properties required by the view
        public List<TextQuestionViewModel> TextQuestions { get; set; } = new List<TextQuestionViewModel>();
        public List<NumericQuestionViewModel> NumericQuestions { get; set; } = new List<NumericQuestionViewModel>();
        public List<BooleanQuestionViewModel> BooleanQuestions { get; set; } = new List<BooleanQuestionViewModel>();
        public List<DailyResponseCount> ResponsesPerDay { get; set; } = new List<DailyResponseCount>();
        public List<ResponseSummaryViewModel> ResponsesList { get; set; } = new List<ResponseSummaryViewModel>();
    }
    
    public class QuestionAggregationViewModel
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public QuestionType Type { get; set; }
        public int Order { get; set; }
        public List<OptionViewModel> Options { get; set; } = new List<OptionViewModel>();
        public Dictionary<string, int> QuestionResults { get; set; } = new Dictionary<string, int>();
    }

    public class TextQuestionViewModel
    {
        public int QuestionId { get; set; }
        public string QuestionTitle { get; set; } = string.Empty;
        public List<TextAnswerViewModel> TextAnswers { get; set; } = new List<TextAnswerViewModel>();
    }

    public class TextAnswerViewModel
    {
        public string Text { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public string? RespondentName { get; set; }
        public string? RespondentEmail { get; set; }
        public int ResponseId { get; set; }
    }

    public class NumericQuestionViewModel
    {
        public int QuestionId { get; set; }
        public string QuestionTitle { get; set; } = string.Empty;
        public double Average { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
    }

    public class BooleanQuestionViewModel
    {
        public int QuestionId { get; set; }
        public string QuestionTitle { get; set; } = string.Empty;
        public int YesCount { get; set; }
        public int Total { get; set; }
    }

    public class DailyResponseCount
    {
        public string Date { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class ResponseSummaryViewModel
    {
        public int Id { get; set; }
        public string RespondentId { get; set; } = string.Empty;
        public string RespondentName { get; set; } = string.Empty;
        public string RespondentEmail { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public DateTime LastModifiedAt { get; set; }
        public int ResponseNumber { get; set; }
    }
} 