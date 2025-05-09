using System.ComponentModel.DataAnnotations;

namespace FormsApp.Models
{
    public class ApiToken
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Token { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastUsed { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Navigation property - links token to user
        [Required]
        public string UserId { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
    }
} 