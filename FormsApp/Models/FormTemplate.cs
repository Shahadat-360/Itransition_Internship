using System.ComponentModel.DataAnnotations;

namespace FormsApp.Models
{
    public class FormTemplate
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        // Make TopicId nullable temporarily for migration
        public int? TopicId { get; set; }
        
        [Obsolete("Use TopicId instead")]
        public string Topic 
        { 
            get => TopicNavigation?.Name ?? "Other"; 
            set { /* No-op, kept for backward compatibility */ } 
        }
        
        public string? ImageUrl { get; set; }
        
        public bool IsPublic { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
        
        // Alias for CreatedAt that can be set for backward compatibility 
        public DateTime DateCreated 
        { 
            get => CreatedAt; 
            set => CreatedAt = value;
        }
        
        // Version for optimistic locking
        [Timestamp]
        public byte[] Version { get; set; } = null!;
        
        // Likes count
        public int LikesCount { get; set; } = 0;
        
        // Navigation properties
        public string CreatorId { get; set; } = null!;
        public virtual ApplicationUser Creator { get; set; } = null!;
        
        public virtual Topic? TopicNavigation { get; set; }
        
        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
        public virtual ICollection<FormResponse> Responses { get; set; } = new List<FormResponse>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<TemplateLike> Likes { get; set; } = new List<TemplateLike>();
        public virtual ICollection<TemplateTag> TemplateTags { get; set; } = new List<TemplateTag>();
        public virtual ICollection<TemplateAccessUser> AllowedUsers { get; set; } = new List<TemplateAccessUser>();
        
        // Define a many-to-many relationship with Tag through TemplateTag
        // This property will be populated by EF Core through the TemplateTags relationship
        public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
    }
} 