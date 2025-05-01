using System.ComponentModel.DataAnnotations;
using FormsApp.Models;
using Microsoft.AspNetCore.Http;

namespace FormsApp.ViewModels
{
    public class FormTemplateViewModel
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string Title { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public int? TopicId { get; set; }
        
        // Keep Topic for backward compatibility
        public string Topic { get; set; } = "Other";
        
        public string? ImageUrl { get; set; }
        
        [Display(Name = "Image")]
        public IFormFile? ImageFile { get; set; }
        
        public bool IsPublic { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        
        public string CreatorName { get; set; } = string.Empty;
        
        public int LikesCount { get; set; }
        
        public int CommentsCount { get; set; }
        
        public bool CurrentUserLiked { get; set; }
        
        // Navigation properties converted to view model properties
        public List<int> TagIds { get; set; } = new List<int>();
        public List<string> Tags { get; set; } = new List<string>();
        
        // Added to handle serialized tags from the form
        public string? TagsJson { get; set; }
        public List<Question>? Questions { get; set; }
        public List<string> AllowedUserEmails { get; set; } = new List<string>();
        
        // Added for allowing emails as a simple string (for textarea)
        public string? AllowedEmails { get; set; }
        
        public string? Version { get; set; }
    }
    
    public class QuestionOptionViewModel
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public int Order { get; set; }
        public int QuestionId { get; set; }
        
        // Convert to OptionViewModel 
        public OptionViewModel ToOptionViewModel()
        {
            return new OptionViewModel
            {
                Id = Id,
                Text = Text,
                Order = Order,
                QuestionId = QuestionId
            };
        }
    }
    
    public class TagViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int UsageCount { get; set; }
    }
} 