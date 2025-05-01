using System.ComponentModel.DataAnnotations;

namespace FormsApp.Models
{
    public class TemplateAccessUser
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int TemplateId { get; set; }
        
        // UserId is no longer required, can be null for email-only access
        public string? UserId { get; set; }
        
        // Email is now required since we need at least one way to identify the user
        [Required]
        public string Email { get; set; } = string.Empty;
        
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual FormTemplate Template { get; set; } = null!;
        public virtual ApplicationUser? User { get; set; }
    }
} 