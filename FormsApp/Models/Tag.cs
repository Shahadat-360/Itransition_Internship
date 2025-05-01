using System.ComponentModel.DataAnnotations;

namespace FormsApp.Models
{
    public class Tag
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;
        
        public int UsageCount { get; set; } = 0;
        
        // Navigation properties
        public virtual ICollection<TemplateTag> TemplateTags { get; set; } = new List<TemplateTag>();
    }
    
    public class TemplateTag
    {
        [Key]
        public int Id { get; set; }
        
        public int TagId { get; set; }
        public virtual Tag Tag { get; set; } = null!;
        
        public int TemplateId { get; set; }
        public virtual FormTemplate Template { get; set; } = null!;
    }
    
    public class TemplateLike
    {
        [Key]
        public int Id { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public int TemplateId { get; set; }
        public virtual FormTemplate Template { get; set; } = null!;
        
        public string UserId { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
    }
} 