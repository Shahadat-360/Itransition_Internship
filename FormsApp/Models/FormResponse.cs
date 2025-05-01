using System.ComponentModel.DataAnnotations;

namespace FormsApp.Models
{
    public class FormResponse
    {
        [Key]
        public int Id { get; set; }
        
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        
        // Backward compatibility properties
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
        public string UserId 
        { 
            get => RespondentId; 
            set => RespondentId = value;
        }
        
        // Version for optimistic locking
        [Timestamp]
        public byte[] Version { get; set; } = null!;
        
        // Navigation properties
        public int TemplateId { get; set; }
        public virtual FormTemplate Template { get; set; } = null!;
        
        public string RespondentId { get; set; } = null!;
        public virtual ApplicationUser Respondent { get; set; } = null!;
        
        public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
    }
} 