using System.ComponentModel.DataAnnotations;

namespace FormsApp.Models
{
    public enum QuestionType
    {
        SingleLineText,
        MultiLineText,
        Integer,
        MultipleChoice,
        Poll
    }

    public class Question
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Text { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [Required]
        public QuestionType Type { get; set; }
        
        public int Order { get; set; }
        
        public bool ShowInResults { get; set; } = true;
        
        // New property to support form validation
        public bool Required { get; set; } = false;
        
        // Navigation properties
        public int TemplateId { get; set; }
        public virtual FormTemplate Template { get; set; } = null!;
        
        public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
        
        // Options for multiple-choice questions
        public virtual ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
    }
} 