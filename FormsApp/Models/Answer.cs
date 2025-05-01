using System.ComponentModel.DataAnnotations;

namespace FormsApp.Models
{
    public class Answer
    {
        [Key]
        public int Id { get; set; }
        
        public string? TextValue { get; set; }
        
        public int? IntValue { get; set; }
        
        public bool? BoolValue { get; set; }
        
        // Backward compatibility properties
        public string Text 
        { 
            get => TextValue ?? string.Empty;
            set => TextValue = value;
        }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public int QuestionId { get; set; }
        public virtual Question Question { get; set; } = null!;
        
        public int ResponseId { get; set; }
        public virtual FormResponse Response { get; set; } = null!;
    }
} 