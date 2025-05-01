using System.ComponentModel.DataAnnotations;

namespace FormsApp.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public int TemplateId { get; set; }
        public virtual FormTemplate Template { get; set; } = null!;
        
        public string AuthorId { get; set; } = null!;
        public virtual ApplicationUser Author { get; set; } = null!;
    }
} 